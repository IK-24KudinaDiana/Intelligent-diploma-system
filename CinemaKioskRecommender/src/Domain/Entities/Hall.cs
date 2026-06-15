using CinemaKioskRecommender.Domain.Common;

namespace CinemaKioskRecommender.Domain.Entities;

public class Hall : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }

    public ICollection<Session> Sessions { get; set; } = new List<Session>();
}
