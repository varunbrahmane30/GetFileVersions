USE [FLCADTest]
GO
/****** Object:  StoredProcedure [dbo].[sp_FLCAD_FILE_VERSION_UpdateFileVersion]    Script Date: 12/10/2022 2:32:13 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_FLCAD_FILE_VERSION_UpdateFileVersion]
(
	@Host nvarchar(250),
	@SoftwareName nvarchar(250),
	@Path nvarchar(500),
	@Last_Update datetime,
	@FileVersion nvarchar(250)
)
AS
SET NOCOUNT ON

BEGIN TRY
	update FileVersions 
	set 
	FileVersion=@FileVersion, Last_Update=@Last_Update,Path=@Path 
	where 
	Host=@Host 
	and
	SoftwareName=@SoftwareName
END TRY

BEGIN CATCH
--	Returns the error information
	DECLARE		@ErrorMessage		NVARCHAR(4000), @ErrorServerity	int
	SELECT		@ErrorMessage		= ERROR_MESSAGE(), @ErrorServerity = ERROR_SEVERITY()
	RAISERROR	(@ErrorMessage,		@ErrorServerity, 1)
END CATCH;



