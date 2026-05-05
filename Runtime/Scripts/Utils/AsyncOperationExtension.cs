using System.Threading.Tasks;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    /// <summary>
    /// Adds task-based awaiting helpers for Unity AsyncOperation instances.
    /// </summary>
    public static class AsyncOperationExtension
    {
        #region Async Operations

        /// <summary>
        /// Asynchronously waits until the operation completes.
        /// </summary>
        /// <param name="operation">Operation to await.</param>
        public static async Task AwaitAsync(this AsyncOperation operation)
        {
            if (operation == null) return;
            while (!operation.isDone)
                await Task.Yield();
        }

        #endregion
    }
}
