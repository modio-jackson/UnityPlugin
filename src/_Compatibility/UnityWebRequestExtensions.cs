using UnityEngine.Networking;

namespace ModIO
{
    #if !UNITY_2017_2_OR_NEWER
    public static class UnityWebRequestAsyncOperationExtensions
    {
        public static ModIO.Compatibility.UnityWebRequestAsyncOperation SendWebRequest(this UnityWebRequest request)
        {
            UnityEngine.AsyncOperation operation = request.Send();

            var operationWrapper = new ModIO.Compatibility.UnityWebRequestAsyncOperation(request, operation);
            return operationWrapper;
        }
    }
    #endif
}
