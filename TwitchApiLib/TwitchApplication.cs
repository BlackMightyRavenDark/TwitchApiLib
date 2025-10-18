
namespace TwitchApiLib
{
	public sealed class TwitchApplication
	{
		public string Name { get; }
		public string Description { get; }
		public string ClientId { get; }
		public string ClientSecretKey { get; }

		public TwitchApplication(string name, string description, string clientId, string clientSecretKey)
		{
			Name = name;
			Description = description;
			ClientId = clientId;
			ClientSecretKey = clientSecretKey;
		}

		public TwitchApplication(TwitchApplication application)
			: this(application.Name, application.Description,
				application.ClientId, application.ClientSecretKey) { }
	}
}
