SELECT 
    staff_code AS Id,
    abbreviation_name AS Name,
    operation_type AS Role
FROM 
    m_tanto 
WHERE 
    staff_type = @StaffType 
    AND staff_code = @StaffCode 
    AND password = @Password 
    AND delete_flag = @DeleteFlag;