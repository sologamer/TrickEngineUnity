using System.Threading.Tasks;

namespace TrickCore
{
    public interface ITrickLoginPlatform<T> where T : ITrickLoginUserData
    {
        /// <summary>
        /// Authenticates the user and returns it's player data. For example this can be using a REST api
        /// </summary>
        /// <param name="userInput">Custom user input used for the server to authenticate. Token/UDID/GUID/User+HashedPass </param>
        /// <returns>Returns the login status and the user data</returns>
        Task<(LoginStatus LoginStatus, T UserData)> Authenticate(string[] userInput);
        
        /// <summary>
        /// On a logout, we can contact the server to do the actual logout, like terminating their active session.
        /// </summary>
        /// <param name="userInput">Custom user input used for the server to log you out.</param>
        /// <returns>Returns true if the user is successfully logged out</returns>
        Task<bool> Logout(string[] userInput);
    }
}