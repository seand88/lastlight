using Godot;
using System;

public static class TextureManager
{
	private static ImageTexture? _atlas;
	public static Texture2D Atlas => _atlas ?? throw new Exception("Atlas not generated");

	public static void GenerateAtlas()
	{
		int size = 256;
		Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		image.Fill(Colors.Transparent);

		void FillRect(int x, int y, int w, int h, Color color)
		{
			for (int ix = x; ix < x + w; ix++)
				for (int iy = y; iy < y + h; iy++)
					if (ix >= 0 && ix < size && iy >= 0 && iy < size)
						image.SetPixel(ix, iy, color);
		}

		// QUADRANT 1 (Top-Left): Small Entity Assets
		FillRect(4, 4, 24, 24, Colors.LightGray); FillRect(8, 10, 4, 6, Colors.Black); FillRect(20, 10, 4, 6, Colors.Black); FillRect(2, 12, 4, 16, Colors.DarkSlateGray); FillRect(26, 12, 4, 12, Colors.Goldenrod); // Player
		FillRect(36, 4, 24, 24, new Color(139/255f, 0, 0)); FillRect(40, 10, 6, 4, Colors.Yellow); FillRect(50, 10, 6, 4, Colors.Yellow); FillRect(32, 20, 32, 4, Colors.Black); // Enemy
		FillRect(64, 0, 32, 32, Colors.DimGray); FillRect(66, 2, 28, 28, Colors.Gray); FillRect(64, 14, 32, 2, Colors.Black); FillRect(80, 0, 2, 14, Colors.Black); FillRect(72, 16, 2, 16, Colors.Black); // Wall
		FillRect(96, 0, 32, 32, new Color(34/255f, 139/255f, 34/255f)); image.SetPixel(100, 2, Colors.LimeGreen); image.SetPixel(120, 25, Colors.LimeGreen); // Grass
		FillRect(8, 40, 16, 20, Colors.White); FillRect(10, 44, 12, 14, Colors.Red); FillRect(12, 36, 8, 4, Colors.SaddleBrown); // Potion
		FillRect(40, 40, 16, 16, Colors.Gold); FillRect(44, 36, 8, 24, Colors.LightYellow); // Weapon Upgrade
		FillRect(64, 32, 32, 32, Colors.SandyBrown); image.SetPixel(70, 35, Colors.SaddleBrown); image.SetPixel(85, 40, Colors.SaddleBrown); // Sand
		FillRect(96, 32, 32, 32, new Color(30/255f, 144/255f, 255/255f)); FillRect(100, 40, 10, 2, Colors.AliceBlue); FillRect(110, 55, 10, 2, Colors.AliceBlue); // Water

		// QUADRANT 3 (Bottom-Left): Large Object Assets
		FillRect(0, 128, 64, 64, Colors.Indigo); FillRect(4, 132, 56, 56, Colors.Purple); FillRect(16, 144, 32, 32, Colors.Black); for(int g=0; g<10; g++) image.SetPixel(32, 144+g, Colors.Magenta); // Spawner
		FillRect(64, 128, 32, 32, Colors.White); FillRect(70, 134, 20, 20, Colors.LightCyan); // Portal

		// QUADRANT 4 (Bottom-Right): Letters
		FillRect(128, 128, 12, 2, Colors.White); FillRect(128, 128, 2, 12, Colors.White); FillRect(128, 134, 8, 2, Colors.White); // 'F'
		FillRect(144, 128, 2, 12, Colors.White); FillRect(144, 128, 8, 2, Colors.White); FillRect(144, 138, 8, 2, Colors.White); FillRect(152, 130, 2, 8, Colors.White); // 'D'
		FillRect(160, 128, 2, 12, Colors.White); FillRect(172, 128, 2, 12, Colors.White); for(int i=0; i<12; i++) image.SetPixel(160+i, 128+i, Colors.White); // 'N'

		// QUADRANT 2 (Top-Right): THE BOSS
		FillRect(128, 0, 128, 128, Colors.DarkSlateBlue);
		FillRect(140, 20, 30, 30, Colors.Yellow); // Eye L
		FillRect(214, 20, 30, 30, Colors.Yellow); // Eye R
		FillRect(128, 80, 128, 20, Colors.Black); // Mouth
		FillRect(128, 0, 20, 40, Colors.Gray); // Horn L
		FillRect(236, 0, 20, 40, Colors.Gray); // Horn R

		_atlas = ImageTexture.CreateFromImage(image);
	}

	public static void SaveTexturesToDisk()
	{
		var image = _atlas.GetImage();
		
		void SaveRegion(string name, Rect2I rect) {
			var region = image.GetRegion(rect);
			region.SavePng($"res://{name}.png");
		}

		SaveRegion("player", new Rect2I(0, 0, 32, 32));
		SaveRegion("enemy", new Rect2I(32, 0, 32, 32));
		SaveRegion("wall", new Rect2I(64, 0, 32, 32));
		SaveRegion("grass", new Rect2I(96, 0, 32, 32));
		SaveRegion("sand", new Rect2I(64, 32, 32, 32));
		SaveRegion("water", new Rect2I(96, 32, 32, 32));
		SaveRegion("potion", new Rect2I(0, 32, 32, 32));
		SaveRegion("weapon_upgrade", new Rect2I(32, 32, 32, 32));
		SaveRegion("spawner", new Rect2I(0, 128, 64, 64));
		SaveRegion("portal", new Rect2I(64, 128, 32, 32));
		SaveRegion("boss", new Rect2I(128, 0, 128, 128));
		
		// Let's create a solid white 4x4 image for the bullet
		var bulletImg = Image.CreateEmpty(4, 4, false, Image.Format.Rgba8);
		bulletImg.Fill(Colors.White);
		bulletImg.SavePng("res://bullet.png");

		image.SavePng("res://atlas.png");

		GD.Print("Saved textures to disk.");
	}

	public static AtlasTexture GetTexture(Rect2 region)
	{
		var tex = new AtlasTexture();
		tex.Atlas = Atlas;
		tex.Region = region;
		return tex;
	}
}
