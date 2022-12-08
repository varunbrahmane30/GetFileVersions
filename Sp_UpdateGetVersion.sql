USE [FLCADTest]
GO
/****** Object:  StoredProcedure [dbo].[sp_UpdateGetFileVersion]    Script Date: 12/8/2022 9:52:34 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_UpdateGetFileVersion]
(
	@FileVersion nvarchar(250),
	@FileName nvarchar(250),
	@Host nvarchar(250)
)
AS
SET NOCOUNT ON

BEGIN TRY
	update FileVersions 
	set FileVersion=@FileVersion 
	where 
	FileName=@FileName 
	and
	Host=@Host
END TRY

BEGIN CATCH
--	Returns the error information
	DECLARE		@ErrorMessage		NVARCHAR(4000), @ErrorServerity	int
	SELECT		@ErrorMessage		= ERROR_MESSAGE(), @ErrorServerity = ERROR_SEVERITY()
	RAISERROR	(@ErrorMessage,		@ErrorServerity, 1)
END CATCH;



