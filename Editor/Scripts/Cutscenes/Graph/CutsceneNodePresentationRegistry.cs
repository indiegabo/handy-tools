using System;
using System.Collections.Generic;
using System.Linq;
using IndieGabo.HandyTools.CutscenesModule.Core;
using IndieGabo.HandyTools.CutscenesModule.Nodes.Meta;
using UnityEditor;
using UnityEngine;

namespace IndieGabo.HandyTools.Editor.CutscenesModule.Graph
{
    public readonly struct CutsceneNodePresentationMetadata
    {
        public CutsceneNodePresentationMetadata(
            Color titleColor,
            Color borderColor,
            Texture2D icon)
        {
            TitleColor = titleColor;
            BorderColor = borderColor;
            Icon = icon;
        }

        public Color TitleColor { get; }

        public Color BorderColor { get; }

        public Texture2D Icon { get; }
    }

    public static class CutsceneNodePresentationRegistry
    {
        private static readonly Dictionary<Type, CutsceneNodePresentationMetadata> Cache = new();
        private static Texture2D _actionIcon;
        private static Texture2D _flowIcon;

        public static CutsceneNodePresentationMetadata GetMetadata(CutsceneNodeBase node)
        {
            return GetMetadata(node?.GetType());
        }

        public static CutsceneNodePresentationMetadata GetMetadata(Type nodeType)
        {
            if (nodeType == null)
            {
                return CreateFlowMetadata();
            }

            if (Cache.TryGetValue(nodeType, out CutsceneNodePresentationMetadata metadata))
            {
                return metadata;
            }

            metadata = BuildMetadata(nodeType);
            Cache[nodeType] = metadata;
            return metadata;
        }

        private static CutsceneNodePresentationMetadata BuildMetadata(Type nodeType)
        {
            if (nodeType == typeof(CutsceneCommentNode))
            {
                return new CutsceneNodePresentationMetadata(
                    new Color(0.56f, 0.29f, 0.09f),
                    new Color(0.92f, 0.61f, 0.23f),
                    LoadIcon("d_TextAsset Icon", "TextAsset Icon", "d_console.infoicon"));
            }

            string category = ResolveCategory(nodeType);

            switch (category)
            {
                case "Actions":
                    return new CutsceneNodePresentationMetadata(
                        new Color(0.11f, 0.28f, 0.36f),
                        new Color(0.33f, 0.64f, 0.82f),
                        GetActionIcon());

                case "Signals":
                    return new CutsceneNodePresentationMetadata(
                        new Color(0.23f, 0.17f, 0.41f),
                        new Color(0.59f, 0.47f, 0.87f),
                        LoadIcon("d_Favorite", "Favorite", "d_console.infoicon"));

                case "Dialogue":
                    return new CutsceneNodePresentationMetadata(
                        new Color(0.39f, 0.17f, 0.32f),
                        new Color(0.82f, 0.43f, 0.72f),
                        LoadIcon("d_UnityEditor.ConsoleWindow", "d_console.infoicon", "console.infoicon"));

                case "Meta":
                    return new CutsceneNodePresentationMetadata(
                        new Color(0.34f, 0.23f, 0.09f),
                        new Color(0.79f, 0.56f, 0.24f),
                        LoadIcon("d_TextAsset Icon", "TextAsset Icon", "d_console.infoicon"));

                default:
                    return CreateFlowMetadata();
            }
        }

        private static CutsceneNodePresentationMetadata CreateFlowMetadata()
        {
            return new CutsceneNodePresentationMetadata(
                new Color(0.22f, 0.22f, 0.22f),
                new Color(0.30f, 0.30f, 0.30f),
                GetFlowIcon());
        }

        private static Texture2D GetActionIcon()
        {
            return _actionIcon ??= LoadIconOrFallback(
                CreatePlayIcon,
                "d_PlayButton",
                "PlayButton",
                "d_PlayButton On",
                GetRenderGraphIconPath("Import"),
                "Profiler.CPU");
        }

        private static Texture2D GetFlowIcon()
        {
            return _flowIcon ??= CreateFlowIcon()
                ?? LoadIcon(
                "Packages/com.unity.shadergraph/Editor/Resources/Icons/sg_graph_icon@2x.png",
                GetRenderGraphIconPath("ScriptLink"),
                GetRenderGraphIconPath("Resources"));
        }

        private static string ResolveCategory(Type nodeType)
        {
            CutsceneNodeMenuAttribute attribute = nodeType
                .GetCustomAttributes(typeof(CutsceneNodeMenuAttribute), false)
                .OfType<CutsceneNodeMenuAttribute>()
                .FirstOrDefault();

            if (attribute == null || string.IsNullOrWhiteSpace(attribute.MenuPath))
            {
                return string.Empty;
            }

            return attribute.MenuPath
                .Split('/')
                .FirstOrDefault() ?? string.Empty;
        }

        private static Texture2D LoadIcon(params string[] iconNames)
        {
            for (int index = 0; index < iconNames.Length; index++)
            {
                string iconName = iconNames[index];

                if (string.IsNullOrWhiteSpace(iconName))
                {
                    continue;
                }

                GUIContent content = EditorGUIUtility.IconContent(iconName);

                if (content?.image is Texture2D contentTexture)
                {
                    return contentTexture;
                }

                Texture2D texture = EditorGUIUtility.FindTexture(iconName);

                if (texture != null)
                {
                    return texture;
                }

                texture = LoadAssetIcon(iconName);

                if (texture != null)
                {
                    return texture;
                }
            }

            return EditorGUIUtility.FindTexture("d_cs Script Icon");
        }

        private static Texture2D LoadIconOrFallback(
            Func<Texture2D> fallbackFactory,
            params string[] iconNames)
        {
            Texture2D icon = LoadIcon(iconNames);

            if (icon != null && icon != EditorGUIUtility.FindTexture("d_cs Script Icon"))
            {
                return icon;
            }

            return fallbackFactory();
        }

        private static Texture2D LoadAssetIcon(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            if (!assetPath.StartsWith("Packages/", StringComparison.Ordinal)
                && !assetPath.StartsWith("Assets/", StringComparison.Ordinal))
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private static string GetRenderGraphIconPath(string iconName)
        {
            return string.Format(
                "Packages/com.unity.render-pipelines.core/Editor/Icons/RenderGraphViewer/{0}{1}@2x.png",
                EditorGUIUtility.isProSkin ? "d_" : string.Empty,
                iconName);
        }

        private static Texture2D CreatePlayIcon()
        {
            Texture2D texture = CreateIconTexture();
            Color strokeColor = ResolveStrokeColor();

            DrawPolyline(
                texture,
                strokeColor,
                1.85f,
                new Vector2(4.7f, 3.1f),
                new Vector2(11.9f, 8.0f),
                new Vector2(4.7f, 12.9f),
                new Vector2(4.7f, 3.1f));

            DrawLine(
                texture,
                strokeColor,
                1.35f,
                new Vector2(6.1f, 4.5f),
                new Vector2(6.1f, 11.5f));

            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFlowIcon()
        {
            Texture2D texture = CreateIconTexture();
            Color strokeColor = ResolveStrokeColor();

            DrawLine(texture, strokeColor, 1.95f, new Vector2(2.7f, 8.0f), new Vector2(5.2f, 8.0f));
            DrawLine(texture, strokeColor, 1.7f, new Vector2(5.0f, 8.0f), new Vector2(8.2f, 4.7f));
            DrawLine(texture, strokeColor, 1.7f, new Vector2(5.0f, 8.0f), new Vector2(8.2f, 11.3f));
            DrawLine(texture, strokeColor, 1.7f, new Vector2(8.2f, 4.7f), new Vector2(11.0f, 8.0f));
            DrawLine(texture, strokeColor, 1.7f, new Vector2(8.2f, 11.3f), new Vector2(11.0f, 8.0f));
            DrawLine(texture, strokeColor, 1.85f, new Vector2(10.6f, 8.0f), new Vector2(13.0f, 8.0f));
            DrawLine(texture, strokeColor, 1.85f, new Vector2(11.3f, 6.4f), new Vector2(13.7f, 8.0f));
            DrawLine(texture, strokeColor, 1.85f, new Vector2(11.3f, 9.6f), new Vector2(13.7f, 8.0f));

            DrawDisc(texture, strokeColor, new Vector2(2.5f, 8.0f), 1.0f);
            DrawDisc(texture, strokeColor, new Vector2(8.2f, 4.7f), 0.95f);
            DrawDisc(texture, strokeColor, new Vector2(8.2f, 11.3f), 0.95f);

            texture.Apply();
            return texture;
        }

        private static void DrawDisc(
            Texture2D texture,
            Color color,
            Vector2 center,
            float radius)
        {
            int minX = Mathf.Max(0, Mathf.FloorToInt(center.x - radius - 1f));
            int maxX = Mathf.Min(15, Mathf.CeilToInt(center.x + radius + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(center.y - radius - 1f));
            int maxY = Mathf.Min(15, Mathf.CeilToInt(center.y + radius + 1f));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 pixelCenter = new(x + 0.5f, y + 0.5f);
                    float distance = Vector2.Distance(pixelCenter, center);

                    if (distance > radius)
                    {
                        continue;
                    }

                    float alpha = Mathf.Clamp01(1f - (distance / radius));
                    BlendPixel(texture, x, y, color, alpha);
                }
            }
        }

        private static Texture2D CreateIconTexture()
        {
            Texture2D texture = new(16, 16, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };

            Color[] pixels = new Color[16 * 16];

            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.clear;
            }

            texture.SetPixels(pixels);
            return texture;
        }

        private static Color ResolveStrokeColor()
        {
            return EditorGUIUtility.isProSkin
                ? new Color(0.83f, 0.86f, 0.90f, 1f)
                : new Color(0.17f, 0.19f, 0.23f, 1f);
        }

        private static void DrawBezier(
            Texture2D texture,
            Color color,
            float width,
            Vector2 start,
            Vector2 startTangent,
            Vector2 endTangent,
            Vector2 end,
            int segments)
        {
            Vector2 previousPoint = start;

            for (int index = 1; index <= segments; index++)
            {
                float t = index / (float)segments;
                Vector2 point = EvaluateBezier(start, startTangent, endTangent, end, t);
                DrawLine(texture, color, width, previousPoint, point);
                previousPoint = point;
            }
        }

        private static Vector2 EvaluateBezier(
            Vector2 start,
            Vector2 startTangent,
            Vector2 endTangent,
            Vector2 end,
            float t)
        {
            float oneMinusT = 1f - t;

            return oneMinusT * oneMinusT * oneMinusT * start
                + 3f * oneMinusT * oneMinusT * t * startTangent
                + 3f * oneMinusT * t * t * endTangent
                + t * t * t * end;
        }

        private static void DrawPolyline(
            Texture2D texture,
            Color color,
            float width,
            params Vector2[] points)
        {
            if (points == null || points.Length < 2)
            {
                return;
            }

            for (int index = 0; index < points.Length - 1; index++)
            {
                DrawLine(texture, color, width, points[index], points[index + 1]);
            }
        }

        private static void DrawLine(
            Texture2D texture,
            Color color,
            float width,
            Vector2 start,
            Vector2 end)
        {
            float radius = Mathf.Max(0.5f, width * 0.5f);
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(start.x, end.x) - radius - 1f));
            int maxX = Mathf.Min(15, Mathf.CeilToInt(Mathf.Max(start.x, end.x) + radius + 1f));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(start.y, end.y) - radius - 1f));
            int maxY = Mathf.Min(15, Mathf.CeilToInt(Mathf.Max(start.y, end.y) + radius + 1f));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    Vector2 pixelCenter = new(x + 0.5f, y + 0.5f);
                    float distance = DistanceToSegment(pixelCenter, start, end);

                    if (distance > radius)
                    {
                        continue;
                    }

                    float alpha = Mathf.Clamp01(1f - (distance / radius));
                    BlendPixel(texture, x, y, color, alpha);
                }
            }
        }

        private static float DistanceToSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            float segmentLengthSquared = segment.sqrMagnitude;

            if (segmentLengthSquared <= Mathf.Epsilon)
            {
                return Vector2.Distance(point, start);
            }

            float t = Mathf.Clamp01(Vector2.Dot(point - start, segment) / segmentLengthSquared);
            Vector2 projection = start + (segment * t);
            return Vector2.Distance(point, projection);
        }

        private static void BlendPixel(Texture2D texture, int x, int y, Color color, float alpha)
        {
            Color current = texture.GetPixel(x, y);
            Color next = Color.Lerp(current, color, alpha);
            next.a = Mathf.Max(current.a, alpha);
            texture.SetPixel(x, y, next);
        }
    }
}