USE [FLCADTest]
GO
/****** Object:  StoredProcedure [dbo].[sp_GetFileVersion]    Script Date: 12/8/2022 9:52:28 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_GetFileVersion]
AS
SET NOCOUNT ON

BEGIN TRY

	select f.computername, f.swname, t.path
		from 
		FLCSystemMember as f, FileVersions as t
		where 
		  f.SWName  like t.FileName
		and
		t.Host = f.ComputerName

END TRY
BEGIN CATCH
--	Returns the error information
	DECLARE		@ErrorMessage		NVARCHAR(4000), @ErrorServerity	int
	SELECT		@ErrorMessage		= ERROR_MESSAGE(), @ErrorServerity = ERROR_SEVERITY()
	RAISERROR	(@ErrorMessage,		@ErrorServerity, 1)
END CATCH;



