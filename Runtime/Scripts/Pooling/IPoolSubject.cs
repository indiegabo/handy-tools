namespace IndieGabo.HandyTools.Pooling
{
    public interface IPoolSubject<TPool>
    {
        void SetPool(TPool pool);
        void OnTakenFromPool();
        void ReleaseToPool();
        void OnReturnedToPool();
    }
}