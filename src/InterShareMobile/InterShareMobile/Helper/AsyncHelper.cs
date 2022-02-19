using System.Threading.Tasks;

namespace InterShareMobile.Helper
{
    public static class AsyncHelper
    {
        public static void RunAndForget(this Task task)
        {
            task.ConfigureAwait(false);
        }
    }
}