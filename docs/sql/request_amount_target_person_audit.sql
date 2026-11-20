-- CRMContextDb ONLY. Run in SSMS against a backup/staging copy first.
-- READ-ONLY audit for 20261112130000_UnifyRequestAmountTargetPerson.
-- It works both before (SupplierId/CashierId) and after (TargetPersonId) the migration.
-- No USE, UPDATE, DELETE, or schema changes are made.
SET NOCOUNT ON;

SELECT DB_NAME() AS [DatabaseBeingAudited];

IF OBJECT_ID(N'dbo.RequestAmounts', N'U') IS NULL
    THROW 51000, 'RequestAmounts does not exist in the selected database. Select CRMContextDb.', 1;

IF COL_LENGTH(N'dbo.RequestAmounts', N'SupplierId') IS NOT NULL
   AND COL_LENGTH(N'dbo.RequestAmounts', N'CashierId') IS NOT NULL
BEGIN
    PRINT N'PRE-MIGRATION: legacy columns are present. All problem result sets should be empty.';

    EXEC sys.sp_executesql N'
        SELECT COUNT(*) AS TotalRequests,
               SUM(CASE WHEN RequestAmountType = 3 AND CashierId IS NOT NULL THEN 1 ELSE 0 END) AS CashierIdsToConvert,
               SUM(CASE WHEN RequestAmountType <> 3 AND SupplierId IS NOT NULL THEN 1 ELSE 0 END) AS SupplierIdsToCopy
        FROM dbo.RequestAmounts;

        -- An old cashier reference can be either Cashier.UserId or Cashier.Id.
        -- The migration intentionally aborts if the mapping is not unique.
        SELECT r.Id, r.RequestNumber, r.CashierId,
               (SELECT COUNT(*) FROM dbo.Cashiers AS c
                WHERE c.Id = r.CashierId OR c.UserId = r.CashierId) AS MatchingCashiers
        FROM dbo.RequestAmounts AS r
        WHERE r.RequestAmountType = 3 AND r.CashierId IS NOT NULL
          AND (SELECT COUNT(*) FROM dbo.Cashiers AS c
               WHERE c.Id = r.CashierId OR c.UserId = r.CashierId) <> 1;

        -- Supplier references must already be Customer entity IDs.
        SELECT r.Id, r.RequestNumber, r.SupplierId
        FROM dbo.RequestAmounts AS r
        WHERE r.RequestAmountType <> 3 AND r.SupplierId IS NOT NULL
          AND NOT EXISTS (SELECT 1 FROM dbo.Customers AS c WHERE c.Id = r.SupplierId);

        -- These are structurally inconsistent old requests; the migration rejects them.
        SELECT r.Id, r.RequestNumber, r.RequestAmountType, r.SupplierId, r.CashierId
        FROM dbo.RequestAmounts AS r
        WHERE (r.RequestAmountType = 3 AND r.SupplierId IS NOT NULL)
           OR (r.RequestAmountType <> 3 AND r.CashierId IS NOT NULL);
    ';
END
ELSE IF COL_LENGTH(N'dbo.RequestAmounts', N'TargetPersonId') IS NOT NULL
BEGIN
    PRINT N'POST-MIGRATION: TargetPersonId is present. Orphan counts should be zero.';

    EXEC sys.sp_executesql N'
        SELECT COUNT(*) AS TotalRequests,
               SUM(CASE WHEN r.TargetPersonId IS NULL THEN 1 ELSE 0 END) AS NoTargetRecorded,
               SUM(CASE WHEN r.RequestAmountType = 3 AND r.TargetPersonId IS NOT NULL AND c.Id IS NULL THEN 1 ELSE 0 END) AS OrphanCashierTargets,
               SUM(CASE WHEN r.RequestAmountType <> 3 AND r.TargetPersonId IS NOT NULL AND p.Id IS NULL THEN 1 ELSE 0 END) AS OrphanSupplierOrCustomerTargets
        FROM dbo.RequestAmounts AS r
        LEFT JOIN dbo.Cashiers AS c
            ON r.RequestAmountType = 3 AND c.Id = r.TargetPersonId
        LEFT JOIN dbo.Customers AS p
            ON r.RequestAmountType <> 3 AND p.Id = r.TargetPersonId;

        -- RequestAmount now uses Cashiers.Id; Accountancy still uses Cashiers.UserId.
        -- Any rows below require investigation. Do not mass-update Accountancy.CashierId.
        SELECT r.Id AS RequestAmountId, r.RequestNumber, a.Id AS AccountancyId,
               a.CashierId AS AccountancyUserId, c.UserId AS ExpectedCashierUserId
        FROM dbo.RequestAmounts AS r
        INNER JOIN dbo.Cashiers AS c ON c.Id = r.TargetPersonId
        INNER JOIN dbo.Accountancies AS a ON a.RequestAmountId = r.Id
        WHERE r.RequestAmountType = 3
          AND a.CashierId IS NOT NULL
          AND a.CashierId <> c.UserId;
    ';
END
ELSE
    THROW 51001, 'Neither the legacy nor new request target columns were found.', 1;
