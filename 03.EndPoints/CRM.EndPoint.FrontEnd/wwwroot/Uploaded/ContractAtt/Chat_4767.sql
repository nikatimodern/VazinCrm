SELECT 
    pf.Id AS PreFactorId,
    pf.SumTotalAmount AS StoredSum,

    SUM(pfi.TotalAmount) AS CalculatedSum
FROM PreFactors pf
INNER JOIN PreFactorItems pfi 

ON pf.Id = pfi.PreFactorId
where pf.IsDeleted = 0 and pfi.IsDeleted=0
GROUP BY pf.Id, pf.SumTotalAmount
HAVING SUM(pfi.TotalAmount) <> pf.SumTotalAmount

