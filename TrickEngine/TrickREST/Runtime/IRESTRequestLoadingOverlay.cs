
namespace TrickCore
{
    public interface IRESTRequestLoadingOverlay
    {
        int RequestCount { get; set; }
        bool Blocked { get; set; }
        void OnStartRequest();
        void OnRequestFinished();
    }
}