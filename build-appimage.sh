#!/bin/bash
set -e

APP_NAME="BookTranslator"
VERSION="1.0.0"

echo "📦 Building AppImage for $APP_NAME v$VERSION..."

# Create AppDir structure
rm -rf AppDir
mkdir -p AppDir/usr/bin
mkdir -p AppDir/usr/share/icons/hicolor/256x256/apps

# Publish the app
echo "🔨 Publishing .NET app..."
cd src
dotnet publish -c Release -r linux-x64 --self-contained true -p:PublishTrimmed=true -o ../AppDir/usr/bin
cd ..

# Copy icon
echo "🎨 Copying icon..."
cp assets/icon.ico AppDir/usr/share/icons/hicolor/256x256/apps/booktranslator.ico
cp assets/icon.ico AppDir/booktranslator.png

# Copy desktop file
cp BookTranslator.desktop AppDir/

# Create AppRun script
cat > AppDir/AppRun << 'EOF'
#!/bin/bash
SELF=$(readlink -f "$0")
HERE=${SELF%/*}
export PATH="${HERE}/usr/bin/:${PATH}"
exec "${HERE}/usr/bin/BookTranslator" "$@"
EOF
chmod +x AppDir/AppRun

# Download appimagetool if not present
if [ ! -f appimagetool-x86_64.AppImage ]; then
    echo "📥 Downloading appimagetool..."
    wget -q https://github.com/AppImage/AppImageKit/releases/download/continuous/appimagetool-x86_64.AppImage
    chmod +x appimagetool-x86_64.AppImage
fi

# Build the AppImage
echo "📦 Creating AppImage..."
ARCH=x86_64 ./appimagetool-x86_64.AppImage AppDir ${APP_NAME}-${VERSION}-x86_64.AppImage

echo "✅ Done! Created: ${APP_NAME}-${VERSION}-x86_64.AppImage"
