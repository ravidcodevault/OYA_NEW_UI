-- Temporary tables only, for the dashboard procedure regression harness.
-- The harness rewrites dbo table references to these fixtures for each execution.
CREATE TABLE #SalesFixtureCustomer (
    FullName nvarchar(200), CustomerTypeRefFullName nvarchar(200),
    BillAddressCity nvarchar(200), BillAddressState nvarchar(200));
CREATE TABLE #SalesFixtureItem (
    ListID nvarchar(50), IsActive bit, IncomeAccountRefFullName nvarchar(200),
    SalesOrPurchaseAccountRefFullName nvarchar(200),
    SalesAndPurchaseIncomeAccountRefFullName nvarchar(200));
CREATE TABLE #SalesFixtureInvoice (
    TxnID nvarchar(50), RefNumber nvarchar(50), CustomerRefFullName nvarchar(200),
    ClassRefFullName nvarchar(200), SalesRepRefFullName nvarchar(200),
    TxnDate date, TimeDeleted datetime, IsPending bit);
CREATE TABLE #SalesFixtureInvoiceLine (
    TxnID nvarchar(50), ItemRefListID nvarchar(50), Amount decimal(18,2),
    InventorySiteRefFullName nvarchar(200), [Desc] nvarchar(200));
SELECT TOP (0) * INTO #SalesFixtureSalesReceipt FROM #SalesFixtureInvoice;
SELECT TOP (0) * INTO #SalesFixtureCreditMemo FROM #SalesFixtureInvoice;
SELECT TOP (0) * INTO #SalesFixtureSalesReceiptLine FROM #SalesFixtureInvoiceLine;
SELECT TOP (0) * INTO #SalesFixtureCreditMemoLine FROM #SalesFixtureInvoiceLine;

INSERT INTO #SalesFixtureCustomer VALUES (N'Fixture Customer', N'Other', N'City', N'State');
INSERT INTO #SalesFixtureItem VALUES
    (N'active', 1, N'Revenue From Sales:Gross Sales:Gross Sales Oya', NULL, NULL),
    (N'inactive', 0, N'Revenue From Sales:Gross Sales:Gross Sales Oya', NULL, NULL);
DECLARE @date date = DATEFROMPARTS(YEAR(GETDATE()), MONTH(GETDATE()), 1);
INSERT INTO #SalesFixtureInvoice VALUES
    (N'invoice', N'I1', N'Fixture Customer', N'public direct', N'Rep', @date, NULL, 0),
    (N'deleted', N'I2', N'Fixture Customer', N'public direct', N'Rep', @date, GETDATE(), 0),
    (N'pending', N'I3', N'Fixture Customer', N'public direct', N'Rep', @date, NULL, 1);
INSERT INTO #SalesFixtureInvoiceLine VALUES
    (N'invoice', N'active', 100, NULL, N'Active item sale'),
    (N'invoice', N'inactive', 200, NULL, N'Historical inactive item sale'),
    (N'invoice', N'inactive', 40, NULL, N'Sales Tax'),
    (N'deleted', N'inactive', 999, NULL, N'Deleted transaction'),
    (N'pending', N'inactive', 999, NULL, N'Pending transaction');
INSERT INTO #SalesFixtureSalesReceipt VALUES
    (N'receipt', N'S1', N'Fixture Customer', N'public direct', N'Rep', @date, NULL, 0);
INSERT INTO #SalesFixtureSalesReceiptLine VALUES
    (N'receipt', N'inactive', 25, NULL, N'Historical inactive item receipt');
INSERT INTO #SalesFixtureCreditMemo VALUES
    (N'credit', N'C1', N'Fixture Customer', N'public direct', N'Rep', @date, NULL, 0);
INSERT INTO #SalesFixtureCreditMemoLine VALUES
    (N'credit', N'inactive', 10, NULL, N'Historical inactive item credit');
-- Old active-only total: 100. Correct historical total: 100 + 200 + 25 - 10 = 315.
