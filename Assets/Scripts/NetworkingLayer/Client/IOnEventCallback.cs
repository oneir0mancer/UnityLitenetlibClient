namespace NetworkingLayer.Client
{
    public interface IOnEventCallback
    {
        void OnEvent(EventPackage eventData);
    }
}
