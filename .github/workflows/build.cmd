@REM 文字コード設定
chcp 65001 > nul

@REM 環境変数の定義
set UNITY_VERSION=6000.0.41f1
set UNITY_EDITOR_PATH=C:\Program Files\Unity\Hub\Editor\
set PROJECT_PATH=C:\Users\vantan\Desktop\September
set EXPORT_PATH="I:\マイドライブ\September\Builds"
set LOG_FILE=%PROJECT_PATH%\logs\sep.log
set GIT_URL="https://github.com/RyuichiroYoshida/September.git"

@REM 出力フォルダを処理のたびに削除する
if exist "%PROJECT_PATH%\Build" (
	rmdir /s /q "%PROJECT_PATH%\Build"
)

@REM プロジェクト更新
cd %PROJECT_PATH%
git pull %GIT_URL%
if not %errorlevel% == 0 (
	exit /b 1
)

@REM Unityビルドコマンドを実行する
"%UNITY_EDITOR_PATH%%UNITY_VERSION%\Editor\Unity.exe" -batchmode -quit -projectPath "%PROJECT_PATH%" -executeMethod BuildCommand.Build -clean -logfile %LOG_FILE% -platform Windows -devmode true -outputPath "%PROJECT_PATH%\Build"

@REM ビルドエラー時にコンソールにログを表示する
if not %errorlevel% == 0 (
	type %LOG_FILE%
	exit /b 1
)

@REM ビルドファイルの圧縮してGoogleDriveへ配置
powershell -NoProfile -ExecutionPolicy Unrestricted Compress-Archive -Path "%PROJECT_PATH%\Build" -DestinationPath "Build.zip" -Force

if not %errorlevel% == 0 (
	exit /b 1
)

move /Y "%PROJECT_PATH%\Build.zip" %EXPORT_PATH%
if not %errorlevel% == 0 (
	exit /b 1
)

pause