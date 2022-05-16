using GitLabTimeManager.Services;

namespace TelegramSender.Reports;

public interface IReportItem<T> 
{
    string User { get; set; }
    T Clone();
}

public class ReportCollectionBase<TItem> : List<TItem> where TItem : IReportItem<TItem>
{
    protected ReportCollectionBase(IEnumerable<TItem> items) : base(items)
    {
    }

    protected ReportCollectionBase()
    {
    }

    public ReportCollectionBase<TItem> Clone()
    {
        return new ReportCollectionBase<TItem>(this.Select(x => x.Clone()));
    }
}

public class ChangesReportItem : IReportItem<ChangesReportItem>, IEqualityComparer<ChangesReportItem>
{
    public string User { get; set; }

    public CommitChanges Changes { get; set; }

    public bool HasChanges => Changes.Additions > 0 || Changes.Deletions > 0;

    public ChangesReportItem Clone()
    {
        return new ChangesReportItem
        {
            User = User,
            Changes = new CommitChanges
            {
                Additions = Changes.Additions,
                Deletions = Changes.Deletions,
            }
        };
    }

    public bool Equals(ChangesReportItem x, ChangesReportItem y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.User == y.User && x.Changes.Equals(y.Changes);
    }

    public int GetHashCode(ChangesReportItem obj)
    {
        return HashCode.Combine(obj.User, obj.Changes);
    }
}

public class ChangesReportCollection : ReportCollectionBase<ChangesReportItem>
{
    public ChangesReportCollection()
    {
    }

    public ChangesReportCollection(IEnumerable<ChangesReportItem> items) : base(items)
    {
    }
}


public class UnemployedReportItem : IReportItem<UnemployedReportItem>, IEqualityComparer<UnemployedReportItem>
{
    public string User { get; set; }

    public UnemployedReportItem Clone()
    {
        return new UnemployedReportItem
        {
            User = User
        };
    }

    public bool Equals(UnemployedReportItem x, UnemployedReportItem y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.User == y.User;
    }

    public int GetHashCode(UnemployedReportItem obj)
    {
        return (obj.User != null ? obj.User.GetHashCode() : 0);
    }
}

public class UnemployedReportCollection : ReportCollectionBase<ChangesReportItem>
{
    public UnemployedReportCollection()
    {
    }

    public UnemployedReportCollection(IEnumerable<ChangesReportItem> items) : base(items)
    {
    }
}


public class IssuesReportItem : IReportItem<IssuesReportItem>, IEqualityComparer<IssuesReportItem>
{
    public string User { get; set; }
    public int Commits { get; set; }
    public int Comments { get; set; }
    public string WebUri { get; set; }
    public int Iid { get; set; }

    public bool IsEmpty => Comments == 0 && Commits == 0;

    public IssuesReportItem Clone()
    {
        return new IssuesReportItem
        {
            User = User,
            Comments = Comments,
            Commits = Commits,
            Iid = Iid,
            WebUri = WebUri,
        };
    }

    public bool Equals(IssuesReportItem x, IssuesReportItem y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (ReferenceEquals(x, null)) return false;
        if (ReferenceEquals(y, null)) return false;
        if (x.GetType() != y.GetType()) return false;
        return x.User == y.User && x.Commits == y.Commits && x.Comments == y.Comments;
    }

    public int GetHashCode(IssuesReportItem obj)
    {
        return HashCode.Combine(obj.User, obj.Commits, obj.Comments);
    }
}

public class IssuesReportCollection : ReportCollectionBase<ChangesReportItem>
{
    public IssuesReportCollection()
    {
    }

    public IssuesReportCollection(IEnumerable<ChangesReportItem> items) : base(items)
    {
    }
}