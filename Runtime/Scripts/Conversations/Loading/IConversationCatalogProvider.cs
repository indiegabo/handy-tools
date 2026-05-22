using System;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using IndieGabo.HandyTools.ConversationsModule.RuntimeData;
using IndieGabo.HandyTools.Utils;
using UnityEngine;
using UnityEngine.Networking;

namespace IndieGabo.HandyTools.ConversationsModule.Loading
{
    /// <summary>
    /// Resolves the lightweight runtime catalog that points to exported conversation payloads.
    /// </summary>
    public interface IConversationCatalogProvider
    {
        /// <summary>
        /// Gets the root directory from which catalog and payload paths are resolved.
        /// </summary>
        string RootPath { get; }

        /// <summary>
        /// Loads the runtime catalog.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded runtime catalog.</returns>
        Task<ConversationRuntimeCatalog> LoadCatalogAsync(
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Resolves the absolute payload path for one catalog entry.
        /// </summary>
        /// <param name="entry">Catalog entry that points to one payload.</param>
        /// <returns>The absolute payload path.</returns>
        string ResolvePayloadPath(ConversationRuntimeCatalog.Entry entry);

        /// <summary>
        /// Clears the cached catalog state so future requests force a reload.
        /// </summary>
        void Invalidate();
    }

    /// <summary>
    /// Loads the Conversations runtime catalog from StreamingAssets-compatible JSON files.
    /// </summary>
    public sealed class StreamingConversationCatalogProvider : IConversationCatalogProvider
    {
        #region Constants

        private const string DefaultRootFolderName = "HandyTools/Conversations";
        private const string CatalogFileName = "catalog.json";

        #endregion

        #region Fields

        private readonly string _rootPath;

        private ConversationRuntimeCatalog _catalog;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one StreamingAssets-backed catalog provider.
        /// </summary>
        /// <param name="rootPath">Optional override for the export root directory.</param>
        public StreamingConversationCatalogProvider(string rootPath = null)
        {
            _rootPath = ResolveRootPath(rootPath);
        }

        #endregion

        #region Public API

        /// <inheritdoc />
        public string RootPath => _rootPath;

        /// <inheritdoc />
        public async Task<ConversationRuntimeCatalog> LoadCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            if (_catalog != null)
            {
                return _catalog;
            }

            string catalogJson = await ConversationStreamingJsonIO.ReadTextAsync(
                Path.Combine(_rootPath, CatalogFileName),
                cancellationToken);

            if (string.IsNullOrWhiteSpace(catalogJson))
            {
                throw new InvalidDataException(
                    $"Conversation catalog at '{_rootPath}' is empty.");
            }

            _catalog = JsonUtility.FromJson<ConversationRuntimeCatalog>(catalogJson);

            if (_catalog == null)
            {
                throw new InvalidDataException(
                    $"Conversation catalog at '{_rootPath}' could not be deserialized.");
            }

            return _catalog;
        }

        /// <inheritdoc />
        public string ResolvePayloadPath(ConversationRuntimeCatalog.Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            string relativePayloadPath = (entry.PayloadPath ?? string.Empty)
                .Replace('/', Path.DirectorySeparatorChar);

            return Path.Combine(_rootPath, relativePayloadPath);
        }

        /// <inheritdoc />
        public void Invalidate()
        {
            _catalog = null;
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves the catalog root path, defaulting to the module StreamingAssets folder.
        /// </summary>
        /// <param name="rootPath">Optional override for the catalog root directory.</param>
        /// <returns>The resolved absolute root path.</returns>
        private static string ResolveRootPath(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                string defaultRelativePath = DefaultRootFolderName.Replace('/', Path.DirectorySeparatorChar);
                return Path.Combine(Application.streamingAssetsPath, defaultRelativePath);
            }

            return Path.IsPathRooted(rootPath)
                ? rootPath
                : Path.Combine(Application.streamingAssetsPath, rootPath);
        }

        #endregion
    }

    /// <summary>
    /// Loads the Conversations runtime catalog from Addressables-managed JSON text assets.
    /// </summary>
    internal sealed class AddressableConversationCatalogProvider : IConversationCatalogProvider
    {
        #region Fields

        private readonly string _catalogKey;

        private ConversationRuntimeCatalog _catalog;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one Addressables-backed catalog provider.
        /// </summary>
        /// <param name="catalogKey">Addressables key used to resolve the shared catalog asset.</param>
        public AddressableConversationCatalogProvider(
            string catalogKey = ConversationAddressablesReflection.DefaultCatalogAddress)
        {
            ConversationAddressablesReflection.EnsureAvailable();
            _catalogKey = string.IsNullOrWhiteSpace(catalogKey)
                ? ConversationAddressablesReflection.DefaultCatalogAddress
                : catalogKey;
        }

        #endregion

        #region Public API

        /// <inheritdoc />
        public string RootPath => _catalogKey;

        /// <inheritdoc />
        public async Task<ConversationRuntimeCatalog> LoadCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            if (_catalog != null)
            {
                return _catalog;
            }

            string catalogJson = await ConversationAddressablesReflection.LoadTextAsync(
                _catalogKey,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(catalogJson))
            {
                throw new InvalidDataException(
                    $"Conversation catalog address '{_catalogKey}' is empty.");
            }

            _catalog = JsonUtility.FromJson<ConversationRuntimeCatalog>(catalogJson);

            if (_catalog == null)
            {
                throw new InvalidDataException(
                    $"Conversation catalog address '{_catalogKey}' could not be deserialized.");
            }

            return _catalog;
        }

        /// <inheritdoc />
        public string ResolvePayloadPath(ConversationRuntimeCatalog.Entry entry)
        {
            if (entry == null)
            {
                throw new ArgumentNullException(nameof(entry));
            }

            if (string.IsNullOrWhiteSpace(entry.PayloadKey))
            {
                throw new InvalidDataException(
                    $"Conversation '{entry.ConversationId}' does not define an Addressables payload key.");
            }

            return entry.PayloadKey;
        }

        /// <inheritdoc />
        public void Invalidate()
        {
            _catalog = null;
        }

        #endregion
    }

    /// <summary>
    /// Tries a primary catalog backend first and falls back to a secondary backend when the primary fails.
    /// </summary>
    internal sealed class FallbackConversationCatalogProvider : IConversationCatalogProvider
    {
        #region Fields

        private readonly IConversationCatalogProvider _primaryProvider;

        private readonly IConversationCatalogProvider _secondaryProvider;

        private IConversationCatalogProvider _lastSuccessfulProvider;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes one fallback catalog provider chain.
        /// </summary>
        /// <param name="primaryProvider">Primary provider attempted first.</param>
        /// <param name="secondaryProvider">Fallback provider attempted after primary failure.</param>
        public FallbackConversationCatalogProvider(
            IConversationCatalogProvider primaryProvider,
            IConversationCatalogProvider secondaryProvider)
        {
            _primaryProvider = primaryProvider ?? throw new ArgumentNullException(nameof(primaryProvider));
            _secondaryProvider = secondaryProvider ?? throw new ArgumentNullException(nameof(secondaryProvider));
        }

        #endregion

        #region Public API

        /// <summary>
        /// Gets the primary catalog backend used by the chain.
        /// </summary>
        public IConversationCatalogProvider PrimaryProvider => _primaryProvider;

        /// <summary>
        /// Gets the fallback catalog backend used by the chain.
        /// </summary>
        public IConversationCatalogProvider SecondaryProvider => _secondaryProvider;

        /// <inheritdoc />
        public string RootPath => _lastSuccessfulProvider?.RootPath
            ?? $"{_primaryProvider.RootPath} -> {_secondaryProvider.RootPath}";

        /// <inheritdoc />
        public async Task<ConversationRuntimeCatalog> LoadCatalogAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                ConversationRuntimeCatalog primaryCatalog =
                    await _primaryProvider.LoadCatalogAsync(cancellationToken);
                _lastSuccessfulProvider = _primaryProvider;
                return primaryCatalog;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception primaryException)
            {
                try
                {
                    ConversationRuntimeCatalog secondaryCatalog =
                        await _secondaryProvider.LoadCatalogAsync(cancellationToken);
                    _lastSuccessfulProvider = _secondaryProvider;
                    return secondaryCatalog;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception secondaryException)
                {
                    throw new InvalidOperationException(
                        "Conversation catalog could not be loaded from either configured backend. "
                        + $"Primary failed: {primaryException.Message} "
                        + $"Fallback failed: {secondaryException.Message}",
                        secondaryException);
                }
            }
        }

        /// <inheritdoc />
        public string ResolvePayloadPath(ConversationRuntimeCatalog.Entry entry)
        {
            IConversationCatalogProvider resolvedProvider =
                _lastSuccessfulProvider ?? _primaryProvider;
            return resolvedProvider.ResolvePayloadPath(entry);
        }

        /// <inheritdoc />
        public void Invalidate()
        {
            _lastSuccessfulProvider = null;
            _primaryProvider.Invalidate();
            _secondaryProvider.Invalidate();
        }

        #endregion
    }

    /// <summary>
    /// Reads exported Conversations JSON files through file I/O or UnityWebRequest when required by the platform path.
    /// </summary>
    internal static class ConversationStreamingJsonIO
    {
        #region Public API

        /// <summary>
        /// Reads one text file from a StreamingAssets-compatible location.
        /// </summary>
        /// <param name="path">Absolute file or request path.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded text content.</returns>
        public static async Task<string> ReadTextAsync(
            string path,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException(
                    "Conversation JSON path cannot be empty.",
                    nameof(path));
            }

            if (RequiresUnityWebRequest(path))
            {
                using UnityWebRequest request = UnityWebRequest.Get(path);
                using CancellationTokenRegistration cancellationRegistration =
                    cancellationToken.Register(request.Abort);

                UnityWebRequestAsyncOperation requestOperation = request.SendWebRequest();
                await requestOperation.AwaitAsync();
                cancellationToken.ThrowIfCancellationRequested();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    throw new IOException(
                        $"Conversation JSON request failed for '{path}': {request.error}");
                }

                return request.downloadHandler?.text ?? string.Empty;
            }

            cancellationToken.ThrowIfCancellationRequested();
            return await Task.Run(() => File.ReadAllText(path), cancellationToken);
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Gets whether the path must be read through UnityWebRequest rather than direct file I/O.
        /// </summary>
        /// <param name="path">Path that should be inspected.</param>
        /// <returns>True when the path represents one request-based location.</returns>
        private static bool RequiresUnityWebRequest(string path)
        {
            return path.Contains("://", StringComparison.Ordinal)
                || path.StartsWith("jar:", StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }

    /// <summary>
    /// Provides reflection-based access to the Addressables runtime APIs so the Conversations base module
    /// can use the backend when the package is present without taking a hard assembly reference.
    /// </summary>
    internal static class ConversationAddressablesReflection
    {
        #region Constants

        public const string DefaultCatalogAddress = "conversations/catalog";

        private const string AddressablesTypeName =
            "UnityEngine.AddressableAssets.Addressables, Unity.Addressables";

        #endregion

        #region Static Fields

        private static readonly Type AddressablesType = Type.GetType(AddressablesTypeName);

        private static readonly MethodInfo LoadAssetAsyncMethod = ResolveLoadAssetAsyncMethod();

        private static readonly MethodInfo ReleaseHandleMethod = ResolveReleaseHandleMethod();

        #endregion

        #region Properties

        /// <summary>
        /// Gets whether the required Addressables runtime APIs are available in the current domain.
        /// </summary>
        public static bool IsAvailable => AddressablesType != null
            && LoadAssetAsyncMethod != null
            && ReleaseHandleMethod != null;

        #endregion

        #region Public API

        /// <summary>
        /// Throws one explicit exception when the Addressables runtime backend is unavailable.
        /// </summary>
        public static void EnsureAvailable()
        {
            if (IsAvailable)
            {
                return;
            }

            throw new NotSupportedException(
                "Conversation Addressables backend requires the Unity Addressables package "
                + "to be installed and available in the current domain.");
        }

        /// <summary>
        /// Loads one Addressables text asset and returns its text contents.
        /// </summary>
        /// <param name="key">Addressables key used to resolve the text asset.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The loaded text content.</returns>
        public static async Task<string> LoadTextAsync(
            string key,
            CancellationToken cancellationToken = default)
        {
            EnsureAvailable();

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException(
                    "Conversation Addressables key cannot be empty.",
                    nameof(key));
            }

            object handle = LoadAssetAsyncMethod
                .MakeGenericMethod(typeof(TextAsset))
                .Invoke(null, new object[] { key });

            if (handle == null)
            {
                throw new InvalidOperationException(
                    $"Conversation Addressables load handle could not be created for key '{key}'.");
            }

            try
            {
                await AwaitHandleAsync(handle, cancellationToken);

                if (!HandleSucceeded(handle))
                {
                    throw new InvalidOperationException(
                        BuildHandleFailureMessage(key, handle));
                }

                TextAsset textAsset = GetHandleResult(handle) as TextAsset;
                return textAsset?.text ?? string.Empty;
            }
            finally
            {
                ReleaseHandle(handle);
            }
        }

        #endregion

        #region Helpers

        /// <summary>
        /// Resolves the generic Addressables load method that accepts one object key.
        /// </summary>
        /// <returns>The resolved method when available.</returns>
        private static MethodInfo ResolveLoadAssetAsyncMethod()
        {
            if (AddressablesType == null)
            {
                return null;
            }

            MethodInfo[] methods = AddressablesType.GetMethods(
                BindingFlags.Public | BindingFlags.Static);

            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];
                ParameterInfo[] parameters = method.GetParameters();

                if (method.Name == "LoadAssetAsync"
                    && method.IsGenericMethodDefinition
                    && parameters.Length == 1
                    && parameters[0].ParameterType == typeof(object))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolves the generic Addressables release method that accepts one async-operation handle.
        /// </summary>
        /// <returns>The resolved method when available.</returns>
        private static MethodInfo ResolveReleaseHandleMethod()
        {
            if (AddressablesType == null)
            {
                return null;
            }

            MethodInfo[] methods = AddressablesType.GetMethods(
                BindingFlags.Public | BindingFlags.Static);

            for (int index = 0; index < methods.Length; index++)
            {
                MethodInfo method = methods[index];

                if (!method.IsGenericMethodDefinition || method.Name != "Release")
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length != 1)
                {
                    continue;
                }

                Type parameterType = parameters[0].ParameterType;

                if (parameterType.IsGenericType
                    && parameterType.Name.StartsWith("AsyncOperationHandle`1", StringComparison.Ordinal))
                {
                    return method;
                }
            }

            return null;
        }

        /// <summary>
        /// Waits for one reflected async-operation handle to complete while honoring cancellation.
        /// </summary>
        /// <param name="handle">Reflected handle instance.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        private static async Task AwaitHandleAsync(
            object handle,
            CancellationToken cancellationToken)
        {
            Task handleTask = GetHandleTask(handle);

            if (handleTask == null)
            {
                throw new InvalidOperationException(
                    "Conversation Addressables handle does not expose a Task property.");
            }

            if (!cancellationToken.CanBeCanceled)
            {
                await handleTask;
                return;
            }

            Task cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
            Task completedTask = await Task.WhenAny(handleTask, cancellationTask);

            if (completedTask != handleTask)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            await handleTask;
        }

        /// <summary>
        /// Gets the reflected Task for one async-operation handle.
        /// </summary>
        /// <param name="handle">Reflected handle instance.</param>
        /// <returns>The reflected Task when available.</returns>
        private static Task GetHandleTask(object handle)
        {
            return handle?.GetType().GetProperty("Task")?.GetValue(handle) as Task;
        }

        /// <summary>
        /// Gets whether the reflected async-operation handle completed successfully.
        /// </summary>
        /// <param name="handle">Reflected handle instance.</param>
        /// <returns>True when the handle status is succeeded.</returns>
        private static bool HandleSucceeded(object handle)
        {
            object status = handle?.GetType().GetProperty("Status")?.GetValue(handle);
            return string.Equals(status?.ToString(), "Succeeded", StringComparison.Ordinal);
        }

        /// <summary>
        /// Builds one readable load-failure message from one reflected handle.
        /// </summary>
        /// <param name="key">Addressables key that was requested.</param>
        /// <param name="handle">Reflected handle instance.</param>
        /// <returns>The formatted failure message.</returns>
        private static string BuildHandleFailureMessage(string key, object handle)
        {
            Exception operationException =
                handle?.GetType().GetProperty("OperationException")?.GetValue(handle) as Exception;
            string status = handle?.GetType().GetProperty("Status")?.GetValue(handle)?.ToString()
                ?? "Unknown";

            if (operationException != null)
            {
                return $"Conversation Addressables load failed for key '{key}': "
                    + operationException.Message;
            }

            return $"Conversation Addressables load failed for key '{key}' with status '{status}'.";
        }

        /// <summary>
        /// Gets the reflected handle result object.
        /// </summary>
        /// <param name="handle">Reflected handle instance.</param>
        /// <returns>The reflected result object.</returns>
        private static object GetHandleResult(object handle)
        {
            return handle?.GetType().GetProperty("Result")?.GetValue(handle);
        }

        /// <summary>
        /// Releases one reflected Addressables handle when possible.
        /// </summary>
        /// <param name="handle">Reflected handle instance.</param>
        private static void ReleaseHandle(object handle)
        {
            if (handle == null || ReleaseHandleMethod == null)
            {
                return;
            }

            try
            {
                Type handleType = handle.GetType();

                if (!handleType.IsGenericType)
                {
                    return;
                }

                Type assetType = handleType.GetGenericArguments()[0];
                ReleaseHandleMethod.MakeGenericMethod(assetType).Invoke(
                    null,
                    new[] { handle });
            }
            catch
            {
                // Releasing one transient load handle must never hide the original load result.
            }
        }

        #endregion
    }
}