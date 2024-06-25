using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DarsyDBCleanUpUsers
{
    public class ViewModels
    {
        public class UserRequestViewModel
        {
            public string[] entityTypes { get; set; }
            public QueryViewModel query { get; set; }
        }

        public class QueryViewModel
        {
            public string queryString { get; set; }
        }

        public class RequestViewModel
        {
            public UserRequestViewModel[] requests { get; set; }
        }

    }
}
