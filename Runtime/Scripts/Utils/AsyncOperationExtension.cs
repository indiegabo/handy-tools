using System.Threading.Tasks;
using UnityEngine;

namespace IndieGabo.HandyTools.Utils
{
    public static class AsyncOperationExtension
    {
        #region Async Operations

        public static async Task AwaitAsync(this AsyncOperation operation)
        {
            if (operation == null) return;
            while (!operation.isDone)
                await Task.Yield();
        }

        #endregion
    }
}
