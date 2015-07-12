using System.Collections;
using System.Collections.Generic;

namespace CommandR.Extensions
{
    public interface IPageable : IQuery
    {
        int? PageNumber { get; set; }
        int? PageSize { get; set; }
    };

    public interface IPagedList
    {
        IEnumerable Items { get; }
    };

    public class PagedList<T> : IPagedList
    {
        public PagedList()
        {
            PageNumber = 1;
            Items = new List<T>();
            TotalItems = 0;
        }

        public int PageNumber { get; set; }
        public IEnumerable<T> Items { get; set; }
        public int TotalItems { get; set; }

        IEnumerable IPagedList.Items
        {
            get { return Items; }
        }
    };
}
