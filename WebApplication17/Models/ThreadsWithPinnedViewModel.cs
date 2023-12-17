using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace WebApplication17.Models
{
    public class ThreadsWithPinnedViewModel
    {
        public IEnumerable<Thread> UnpinnedThreads { get; set; }
        public IEnumerable<Thread> PinnedThreads { get; set; }
    }
}