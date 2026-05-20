using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services;

public class DebtSimplificationService {

    public List<SettlementSuggestion> Simplify(List<(int UserId, string Name, decimal Balance)> balances) {
        return new List<SettlementSuggestion>();
    }
}