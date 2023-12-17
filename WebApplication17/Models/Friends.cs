using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace WebApplication17.Models
{
    public class Friends
    {
        public int Id { get; set; }
        public virtual ApplicationUser User { get; set; }
        public virtual ApplicationUser Friend { get; set; }
    }
}