namespace E_Commers.DtoModels.TokenDtos
{
	public class TokensDto
	{
		public string Userid { get; set; }
		public string Token { get; set; }
		public string RefreshToken { get; set; }
		public TokensDto()
		{
			
		}
		public TokensDto(string userid,string token,string refreshtoken)
		{
			Userid = userid;
			Token = token;
			RefreshToken = refreshtoken;
			
		}
	}
}
