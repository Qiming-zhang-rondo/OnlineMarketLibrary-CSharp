

namespace OnlineMarket.OrleansImpl.Interfaces;


    /// <summary>
    /// “卖家视图” Grain 接口。当前完全等同于 ISellerActor，
    /// 如需额外只读查询，后续可在此接口继续添加。
    /// </summary>
    public interface ISellerViewActor : ISellerActor
    {
        // 现在不加新成员，纯继承即可。
    }
