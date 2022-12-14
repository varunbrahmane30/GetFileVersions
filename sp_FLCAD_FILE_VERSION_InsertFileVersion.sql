USE [FLCADTest]
GO
/****** Object:  StoredProcedure [dbo].[sp_FLCAD_FILE_VERSION_InsertFileVersion]    Script Date: 12/10/2022 1:34:41 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[sp_FLCAD_FILE_VERSION_InsertFileVersion]
(
	@Host nvarchar(250),
	@SoftwareName nvarchar(250),
	@Path nvarchar(500),
	@Last_Update datetime,
	@Database_Name nvarchar(250)

)
AS
SET NOCOUNT ON

BEGIN TRY
	insert into FileVersions
	(Host, SoftwareName,Path,Last_Update,Database_Name)
	values
	(@Host, @SoftwareName, @Path,@Last_Update,@Database_Name)
END TRY

BEGIN CATCH
--	Returns the error information
	DECLARE		@ErrorMessage		NVARCHAR(4000), @ErrorServerity	int
	SELECT		@ErrorMessage		= ERROR_MESSAGE(), @ErrorServerity = ERROR_SEVERITY()
	RAISERROR	(@ErrorMessage,		@ErrorServerity, 1)
END CATCH;



