using epmgr.Data;

namespace epmgr.Model
{
    public abstract class YateUserModel<T>:IYateUserModel<T> where T:YateUser
    {
        public T User { get; set; }
    }

    public interface IYateUserModel<out T> where T : YateUser
    {
        T User { get; }
    }

    public class DectUserModel:YateUserModel<DectUser>
    {
        public string Token { get; set; }
    }

    public class SipUserModel:YateUserModel<SipUser>
    {
    }

    public class PremiumUserModel:YateUserModel<PremiumUser>
    {
    }

    public class GsmUserModel:YateUserModel<GsmUser>
    {
    }
}