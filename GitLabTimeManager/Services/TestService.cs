using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitLabTimeManager.Services
{
    public interface ITestService
    {
        bool ItsWork { get; }
    }
    public class TestService : ITestService
    {
        public bool ItsWork { get; }
        
        public TestService()
        {
            ItsWork = true;
        }
    }
}
