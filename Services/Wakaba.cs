using Wakaba2ChApiClient.Impl;

namespace vkbot_vitalya.Services;

public class Wakaba {
    /// <summary>
    /// Если требуется прокси
    /// </summary>=
    public Wakaba(HttpClientHandler handler = null) {
        Wakaba2ChApiClient = handler != null
            ? Wakaba2ChApiClient = new Wakaba2ChApi(new HttpClient(handler))
            : Wakaba2ChApiClient = new Wakaba2ChApi(new HttpClient());
    }


    private Wakaba2ChApi Wakaba2ChApiClient { get; set; }

    public async Task<List<Wakaba2ChApiClient.Models.Thread>> GetThreads() {
        var threadsFromBoard = await Wakaba2ChApiClient.GetAllThreadsFromBoardLite("b");
        var ordered = threadsFromBoard.Threads.OrderByDescending(x => x.Score);

        return ordered.ToList();
    }
}