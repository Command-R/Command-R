using System.Collections.Generic;

namespace CommandR.Authentication
{
    public interface ITokenService
    {
        string CreateToken(IDictionary<string, object> data);
        IDictionary<string, object> GetTokenData(string tokenId);
        void DeleteToken(string tokenId);
    };
}
