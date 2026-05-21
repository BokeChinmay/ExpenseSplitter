using ExpenseSplitter.Api.Models;

namespace ExpenseSplitter.Api.Services;

public class DebtSimplificationService {

    public List<SettlementSuggestion> Simplify(List<(int UserId, string Name, decimal Balance)> balances) {
        
        var settlements = new List<SettlementSuggestion>();

        //Separate into creditors (positive balances) and debtors (negative balances)
        var creditors = balances.Where(b => b.Balance > 0).Select(b => (b.UserId, b.Name, Balance: b.Balance)).OrderByDescending(b => b.Balance).ToList();

        var debtors = balances.Where(b => b.Balance < 0).Select(b => (b.UserId, b.Name, Balance: Math.Abs(b.Balance))).OrderByDescending(b => b.Balance).ToList();

        int i = 0, j = 0;

        while (i < creditors.Count && j < debtors.Count) {
            var creditor = creditors[i];
            var debtor = debtors[j];

            //The settlement amount is the smaller of the two balances
            var amount = Math.Min(creditor.Balance, debtor.Balance);

            if (amount > 0.01m) {
                settlements.Add(new SettlementSuggestion(
                    FromUserId: debtor.UserId,
                    FromUserName: debtor.Name,
                    ToUserId: creditor.UserId,
                    ToUserName: creditor.Name,
                    Amount: Math.Round(amount, 2)
                ));
            }

            //Reduce balances by the settled amount
            creditors[i] = creditor with { Balance = creditor.Balance - amount };
            debtors[j] = debtor with { Balance = debtor.Balance - amount};

            //Move to next credtiro or debtor when their balance hits zero
            if (creditors[i].Balance <= 0.01m) i++;
            if (debtors[j].Balance <= 0.01m) j++;
        }

        return settlements;
    }

    public List<(int UserId, string Name, decimal Balance)> CalculateBalances(
        List<(int UserId, string Name)> members, 
        List<(int PaidByUserId, decimal Amount, List<(int UserId, decimal OwedAmount)> Splits)> expenses) 
        {
            //Initialize everyone at zero
            var balances = members.ToDictionary(
                m => m.UserId,
                m => (m.UserId, m.Name, Balance: 0m)
            );

            foreach (var expense in expenses) {
                //The person who paid gets credited the full amount
                if (balances.ContainsKey(expense.PaidByUserId)) {
                    var current = balances[expense.PaidByUserId];
                    balances[expense.PaidByUserId] = current with {
                        Balance = current.Balance + expense.Amount
                    };
                }

                //Each person in the split gets debited their share
                foreach (var split in expense.Splits) {
                    if (balances.ContainsKey(split.UserId)) {
                        var current = balances[split.UserId];
                        balances[split.UserId] = current with {
                            Balance = current.Balance - split.OwedAmount
                        };
                    }
                }
            }

            return balances.Values.ToList();
        }
}