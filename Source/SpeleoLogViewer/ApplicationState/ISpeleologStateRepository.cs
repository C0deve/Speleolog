using System.Threading;
using System.Threading.Tasks;

namespace SpeleoLogViewer.ApplicationState;

public interface ISpeleologStateRepository
{
    Task SaveAsync(SpeleologState state, CancellationToken token = default);
    Task<SpeleologState?> GetAsync(CancellationToken token = default);
}