namespace Apollo.Common.Abstractions
{
	public enum ApolloQueue
	{
		/// <summary>
		/// The queue used for registration of clients and aliases
		/// </summary>
		Registrations, 
		/// <summary>
		/// The queue which is used to issue server requests
		/// </summary>
		ServerRequests,
		/// <summary>
		/// The queue which you send to to communicate with clients via aliases
		/// </summary>
		Aliases,
		/// <summary>
		/// The direct to client sessions
		/// </summary>
		ClientSessions
	}
}