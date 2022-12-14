USE [FLCADTest]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetFileVersion]    Script Date: 12/8/2022 9:52:28 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_FLCAD_FILE_VERSION_GetFileVersion]
(
	@ComputerName varchar(250)
)
AS
SET NOCOUNT ON

BEGIN TRY

select SWName 
from 
FLCSystemMember 
where ComputerName=@ComputerName 
and 
SWVersion is not null

END TRY
BEGIN CATCH
--	Returns the error information
	DECLARE		@ErrorMessage		NVARCHAR(4000), @ErrorServerity	int
	SELECT		@ErrorMessage		= ERROR_MESSAGE(), @ErrorServerity = ERROR_SEVERITY()
	RAISERROR	(@ErrorMessage,		@ErrorServerity, 1)
END CATCH;



