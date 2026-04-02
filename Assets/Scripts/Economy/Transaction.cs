using System;

namespace Economy
{
    /// <summary>
    /// Represents the type of financial transaction.
    /// </summary>
    public enum TransactionType
    {
        /// <summary>Money coming in (inspection fees, tips, bonuses)</summary>
        Income,
        /// <summary>Money going out (purchases, expenses)</summary>
        Expense
    }

    /// <summary>
    /// Categories for organizing transactions.
    /// </summary>
    public enum TransactionCategory
    {
        // Income Categories
        /// <summary>Base inspection fee income</summary>
        Inspection,
        /// <summary>Advanced/specialized inspection income</summary>
        AdvancedInspection,
        /// <summary>Customer tips based on satisfaction</summary>
        Tip,
        /// <summary>Bonus payments (accuracy, speed, etc.)</summary>
        Bonus,
        /// <summary>Other miscellaneous income</summary>
        OtherIncome,

        // Expense Categories
        /// <summary>Tool purchases from the store</summary>
        ToolPurchase,
        /// <summary>Equipment and upgrade purchases</summary>
        Upgrade,
        /// <summary>Daily workshop rent</summary>
        Rent,
        /// <summary>Employee salary payments</summary>
        Salary,
        /// <summary>Repair and maintenance costs</summary>
        Maintenance,
        /// <summary>Supply purchases (consumables)</summary>
        Supplies,
        /// <summary>Penalty or fine payments</summary>
        Penalty,
        /// <summary>Other miscellaneous expenses</summary>
        OtherExpense
    }

    /// <summary>
    /// Represents a single financial transaction record.
    /// Tracks all money flow in the game including income and expenses.
    /// </summary>
    [Serializable]
    public class Transaction
    {
        /// <summary>
        /// Unique identifier for this transaction.
        /// </summary>
        public string TransactionId { get; private set; }

        /// <summary>
        /// Whether this is income or an expense.
        /// </summary>
        public TransactionType Type { get; private set; }

        /// <summary>
        /// Category for grouping and filtering transactions.
        /// </summary>
        public TransactionCategory Category { get; private set; }

        /// <summary>
        /// The monetary amount (always positive, use Type to determine direction).
        /// </summary>
        public float Amount { get; private set; }

        /// <summary>
        /// Description of what this transaction represents.
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// When this transaction occurred.
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// Game day when this transaction occurred.
        /// </summary>
        public int GameDay { get; private set; }

        /// <summary>
        /// Balance after this transaction was applied.
        /// </summary>
        public float BalanceAfter { get; private set; }

        /// <summary>
        /// Optional reference ID (e.g., inspection ID, item ID).
        /// </summary>
        public string ReferenceId { get; private set; }

        /// <summary>
        /// Creates a new transaction record.
        /// </summary>
        /// <param name="type">Income or Expense</param>
        /// <param name="category">Transaction category</param>
        /// <param name="amount">The monetary amount (must be positive)</param>
        /// <param name="description">Description of the transaction</param>
        /// <param name="gameDay">Current game day</param>
        /// <param name="balanceAfter">Balance after transaction</param>
        /// <param name="referenceId">Optional reference ID</param>
        public Transaction(
            TransactionType type,
            TransactionCategory category,
            float amount,
            string description,
            int gameDay,
            float balanceAfter,
            string referenceId = null)
        {
            if (amount < 0)
            {
                throw new ArgumentException("Transaction amount must be positive. Use Type to indicate direction.", nameof(amount));
            }

            TransactionId = Guid.NewGuid().ToString();
            Type = type;
            Category = category;
            Amount = amount;
            Description = description ?? string.Empty;
            Timestamp = DateTime.Now;
            GameDay = gameDay;
            BalanceAfter = balanceAfter;
            ReferenceId = referenceId;
        }

        /// <summary>
        /// Parameterless constructor for serialization.
        /// </summary>
        public Transaction()
        {
            TransactionId = Guid.NewGuid().ToString();
            Timestamp = DateTime.Now;
        }

        /// <summary>
        /// Returns a formatted string representation of this transaction.
        /// </summary>
        public override string ToString()
        {
            string sign = Type == TransactionType.Income ? "+" : "-";
            return $"[{Timestamp:yyyy-MM-dd HH:mm}] Day {GameDay}: {sign}${Amount:F2} - {Category} - {Description}";
        }

        /// <summary>
        /// Checks if this transaction is an income type.
        /// </summary>
        public bool IsIncome => Type == TransactionType.Income;

        /// <summary>
        /// Checks if this transaction is an expense type.
        /// </summary>
        public bool IsExpense => Type == TransactionType.Expense;
    }

    /// <summary>
    /// Data transfer object for transaction serialization.
    /// </summary>
    [Serializable]
    public class TransactionDTO
    {
        public string transactionId;
        public int type;
        public int category;
        public float amount;
        public string description;
        public string timestamp;
        public int gameDay;
        public float balanceAfter;
        public string referenceId;

        /// <summary>
        /// Converts a Transaction to a DTO for serialization.
        /// </summary>
        public static TransactionDTO FromTransaction(Transaction transaction)
        {
            return new TransactionDTO
            {
                transactionId = transaction.TransactionId,
                type = (int)transaction.Type,
                category = (int)transaction.Category,
                amount = transaction.Amount,
                description = transaction.Description,
                timestamp = transaction.Timestamp.ToString("o"),
                gameDay = transaction.GameDay,
                balanceAfter = transaction.BalanceAfter,
                referenceId = transaction.ReferenceId
            };
        }

        /// <summary>
        /// Converts this DTO back to a Transaction object.
        /// </summary>
        public Transaction ToTransaction()
        {
            return new Transaction(
                (TransactionType)type,
                (TransactionCategory)category,
                amount,
                description,
                gameDay,
                balanceAfter,
                referenceId)
            {
                Timestamp = DateTime.Parse(timestamp)
            };
        }
    }
}
