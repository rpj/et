namespace ET
{
    public interface IChannelSubscriber
    {
        void NewMessage(string channel, string message);
    }
}