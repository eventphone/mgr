using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace epmgr.Model
{
    public class ListModelBase:PageModel
    {
        protected ListModelBase()
        {
            CurrentPage = 1;
            PageSize = 50;
        }

        [BindProperty(Name="q", SupportsGet = true)]
        public string Search { get; set; }

        [BindProperty(Name="p", SupportsGet = true)]
        public int CurrentPage { get; set; }

        private long _total;

        public long Total
        {
            get { return _total; }
            protected set
            {
                _total = value;
                if (LastPage)
                {
                    CurrentPage = (int)(Total / PageSize);
                    if (!LastPage)
                        CurrentPage++;
                    if (CurrentPage < 1)
                        CurrentPage = 1;
                }
            }
        }

        public int PageCount
        {
            get
            {
                return (int)Math.Ceiling(Total / (double)PageSize); }
        }

        public int PageSize { get; protected set; }

        public bool LastPage
        {
            get { return CurrentPage * (long)PageSize >= Total; }
        }

        public virtual IDictionary<string, string> Parameters
        {
            get
            {
                return new Dictionary<string, string>
                {
                    {"q", Search},
                    {"p", CurrentPage != 1 ? CurrentPage.ToString() : null}
                };
            }
        }
    }
}
