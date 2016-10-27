﻿// ClassicalSharp copyright 2014-2016 UnknownShadow200 | Licensed under MIT
using System;
using System.Drawing;
using OpenTK.Input;
#if ANDROID
using Android.Graphics;
#endif

namespace ClassicalSharp.Gui.Widgets {
	public sealed class InputWidget : Widget {
		
		internal const int lines = 3;
		public InputWidget( Game game, Font font ) : base( game ) {
			HorizontalAnchor = Anchor.LeftOrTop;
			VerticalAnchor = Anchor.BottomOrRight;
			typingLogPos = game.Chat.InputLog.Count; // Index of newest entry + 1.			
			buffer = new WrappableStringBuffer( Utils.StringLength * lines );
			
			DrawTextArgs args = new DrawTextArgs( "_", font, true );
			caretTex = game.Drawer2D.MakeChatTextTexture( ref args, 0, 0 );
			caretTex.Width = (short)((caretTex.Width * 3) / 4);
			defaultCaretWidth = caretTex.Width;
			
			args = new DrawTextArgs( "> ", font, true );
			Size defSize = game.Drawer2D.MeasureChatSize( ref args );
			defaultWidth = Width = defSize.Width;
			defaultHeight = Height = defSize.Height;
			
			this.font = font;
			inputHandler = new InputWidgetHandler( game, this );
		}
		
		InputWidgetHandler inputHandler;
		internal Texture inputTex, caretTex, prefixTex;
		internal int caretPos = -1, typingLogPos = 0;
		internal int defaultCaretWidth, defaultWidth, defaultHeight;
		internal WrappableStringBuffer buffer;
		internal readonly Font font;
		internal string originalText;

		FastColour caretCol;
		static FastColour backColour = new FastColour( 0, 0, 0, 127 );
		public override void Render( double delta ) {
			gfx.Texturing = false;
			int y = Y, x = X;
			
			for( int i = 0; i < sizes.Length; i++ ) {
				if( i > 0 && sizes[i].Height == 0 ) break;
				bool caretAtEnd = caretTex.Y1 == y && (indexX == LineLength || caretPos == -1);				
				int drawWidth = sizes[i].Width + (caretAtEnd ? caretTex.Width : 0);
				// Cover whole window width to match original classic behaviour
				if( game.PureClassic )
					drawWidth = Math.Max( drawWidth, game.Width - X * 4 );
				
				gfx.Draw2DQuad( x, y, drawWidth + 10, defaultHeight, backColour );
				y += sizes[i].Height;				
			}
			gfx.Texturing = true;
			                 
			inputTex.Render( gfx );
			caretTex.Render( gfx, caretCol );
		}

		internal string[] parts = new string[lines];
		int[] partLens = new int[lines];
		Size[] sizes = new Size[lines];
		int maxWidth = 0;
		int indexX, indexY;
		bool shownWarning;
		
		internal int LineLength { get { return game.Server.SupportsPartialMessages ? 64 : 62; } }
		internal int TotalChars { get { return LineLength * lines; } }
		
		public override void Init() {
			X = 5;
			buffer.WordWrap( game.Drawer2D, ref parts, ref partLens, 
			                       LineLength, TotalChars );

			for( int y = 0; y < sizes.Length; y++ )
				sizes[y] = Size.Empty;
			sizes[0].Width = defaultWidth; maxWidth = defaultWidth;
				
			DrawTextArgs args = new DrawTextArgs( null, font, true );
			for( int y = 0; y < lines; y++ ) {
				int offset = y == 0 ? defaultWidth : 0;
				args.Text = parts[y];
				sizes[y] += game.Drawer2D.MeasureChatSize( ref args );
				maxWidth = Math.Max( maxWidth, sizes[y].Width );
			}
			if( sizes[0].Height == 0 ) sizes[0].Height = defaultHeight;
			
			bool supports = game.Server.SupportsPartialMessages;
			if( buffer.Length > LineLength && !shownWarning && !supports ) {
				game.Chat.Add( "&eNote: Each line will be sent as a separate packet.", MessageType.ClientStatus6 );
				shownWarning = true;
			} else if( buffer.Length <= LineLength && shownWarning ) {
				game.Chat.Add( null, MessageType.ClientStatus6 );
				shownWarning = false;
			}
			
			DrawString();
			CalculateCaretData();
		}
		
		internal void CalculateCaretData() {
			if( caretPos >= buffer.Length ) caretPos = -1;
			buffer.MakeCoords( caretPos, partLens, out indexX, out indexY );
			DrawTextArgs args = new DrawTextArgs( null, font, true );

			if( indexX == LineLength ) {
				caretTex.X1 = 10 + sizes[indexY].Width;
				caretCol = FastColour.Yellow;
				caretTex.Width = (short)defaultCaretWidth;
			} else {
				args.Text = parts[indexY].Substring( 0, indexX );
				Size trimmedSize = game.Drawer2D.MeasureChatSize( ref args );
				if( indexY == 0 ) trimmedSize.Width += defaultWidth;
				caretTex.X1 = 10 + trimmedSize.Width;
				caretCol = FastColour.Scale( FastColour.White, 0.8f );
				
				string line = parts[indexY];
				args.Text = indexX < line.Length ? new String( line[indexX], 1 ) : "";
				int caretWidth = indexX < line.Length ? 
					game.Drawer2D.MeasureChatSize( ref args ).Width : defaultCaretWidth;
				caretTex.Width = (short)caretWidth;
			}
			caretTex.Y1 = sizes[0].Height * indexY + Y;
			CalcCaretColour();
		}
		
		void DrawString() {
			int totalHeight = 0;
			for( int i = 0; i < lines; i++ )
				totalHeight += sizes[i].Height;
			Size size = new Size( maxWidth, totalHeight );
			
			int realHeight = 0;
			using( Bitmap bmp = IDrawer2D.CreatePow2Bitmap( size ) )
				using( IDrawer2D drawer = game.Drawer2D )
			{
				drawer.SetBitmap( bmp );
				DrawTextArgs args = new DrawTextArgs( "> ", font, true );
				drawer.DrawChatText( ref args, 0, 0 );
				
				for( int i = 0; i < parts.Length; i++ ) {
					if( parts[i] == null ) break;
					args.Text = parts[i];
					char lastCol = GetLastColour( 0, i );
					if( !IDrawer2D.IsWhiteColour( lastCol ) )
						args.Text = "&" + lastCol + args.Text;
					
					int offset = i == 0 ? defaultWidth : 0;
					drawer.DrawChatText( ref args, offset, realHeight );
					realHeight += sizes[i].Height;
				}
				inputTex = drawer.Make2DTexture( bmp, size, 10, 0 );
			}
			
			Height = realHeight == 0 ? defaultHeight : realHeight;
			Y = game.Height - Height - YOffset;
			inputTex.Y1 = Y;
			Width = size.Width;
		}
		
		void CalcCaretColour() {
			IDrawer2D drawer = game.Drawer2D;
			char code = GetLastColour( indexX, indexY );
			if( code != '\0' )
				caretCol = drawer.Colours[code];
		}
		
		char GetLastColour( int indexX, int indexY ) {
			int x = indexX;
			IDrawer2D drawer = game.Drawer2D;
			for( int y = indexY; y >= 0; y-- ) {
				string part = parts[y];
				char code = drawer.LastColour( part, x );
				if( code != '\0' ) return code;
				if( y > 0 ) x = parts[y - 1].Length;
			}
			return '\0';
		}

		public override void Dispose() {
			gfx.DeleteTexture( ref inputTex );
		}
		
		public void DisposeFully() {
			Dispose();
			gfx.DeleteTexture( ref caretTex );
			gfx.DeleteTexture( ref prefixTex );
		}

		public override void MoveTo( int newX, int newY ) {
			int dx = newX - X, dy = newY - Y;
			X = newX; Y = newY;
			caretTex.Y1 += dy;
			inputTex.Y1 += dy;
		}
		
		public void SendTextInBufferAndReset() {
			SendInBuffer();
			typingLogPos = game.Chat.InputLog.Count; // Index of newest entry + 1.
			buffer.Clear();
			caretPos = -1;
			Dispose();
			Height = defaultHeight;
			originalText = null;
			
			game.Chat.Add( null, MessageType.ClientStatus4 );
			game.Chat.Add( null, MessageType.ClientStatus5 );
			game.Chat.Add( null, MessageType.ClientStatus6 );
		}
		
		void SendInBuffer() {
			if( buffer.Empty ) return;
			// Don't want trailing spaces in output message
			string allText = new String( buffer.value, 0, buffer.TextLength );
			game.Chat.InputLog.Add( allText );
			
			if( game.Server.SupportsPartialMessages )
				SendWithPartial( allText );
			else
				SendNormal();
		}
		
		void SendWithPartial( string allText ) {
			// don't automatically word wrap the message.
			while( allText.Length > Utils.StringLength ) {
				game.Chat.Send( allText.Substring( 0, Utils.StringLength ), true );
				allText = allText.Substring( Utils.StringLength );
			}
			game.Chat.Send( allText, false );
		}
		
		void SendNormal() {
			int packetsCount = 0;
			for( int i = 0; i < parts.Length; i++ ) {
				if( parts[i] == null ) break;
				packetsCount++;
			}
			
			// split up into both partial and final packet.
			for( int i = 0; i < packetsCount - 1; i++ )
				SendNormalText( i, true );
			SendNormalText( packetsCount - 1, false );
		}
		
		void SendNormalText( int i, bool partial ) {
			string text = parts[i];
			char lastCol = GetLastColour( 0, i );
			if( !IDrawer2D.IsWhiteColour( lastCol ) )
				text = "&" + lastCol + text;
			game.Chat.Send( text, partial );
		}
		
		public void Clear() {
			buffer.Clear();
			for( int i = 0; i < parts.Length; i++ ) {
				parts[i] = null;
			}
		}
		
		public void AppendText( string text ) {
			if( buffer.Length + text.Length > TotalChars ) {
				text = text.Substring( 0, TotalChars - buffer.Length );
			}
			if( text == "" ) return;
			
			if( caretPos == -1 ) {
				buffer.InsertAt( buffer.Length, text );
			} else {
				buffer.InsertAt( caretPos, text );
				caretPos += text.Length;
				if( caretPos >= buffer.Length ) caretPos = -1;
			}
			Recreate();
		}
		
		public void AppendChar( char c ) {
			if( buffer.Length == TotalChars ) return;
			
			if( caretPos == -1 ) {
				buffer.InsertAt( buffer.Length, c );
			} else {
				buffer.InsertAt( caretPos, c );
				caretPos++;
				if( caretPos >= buffer.Length ) caretPos = -1;
			}
			Recreate();
		}
		
		
		public override bool HandlesKeyPress( char key ) {
			if( game.HideGui ) return true;
			
			if( Utils.IsValidInputChar( key, game ) ) {
				AppendChar( key );
				return true;
			}
			return false;
		}
		
		public override bool HandlesKeyDown( Key key ) {
			if( game.HideGui ) return key < Key.F1 || key > Key.F35;
			return inputHandler.HandlesKeyDown( key );
		}
		
		public override bool HandlesMouseClick( int mouseX, int mouseY, MouseButton button ) {
			return inputHandler.HandlesMouseClick( mouseX, mouseY, button );
		}
	}
}