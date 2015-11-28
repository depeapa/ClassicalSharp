﻿using System;
using System.Diagnostics;
using System.IO;
using ClassicalSharp;

namespace Launcher2 {

	public static class Client {
		
		public static bool Start( ClientStartData data, bool classicubeSkins, ref bool shouldExit ) {
			string skinServer = classicubeSkins ? "http://www.classicube.net/static/skins/" :
				"http://s3.amazonaws.com/MinecraftSkins/";
			string args = data.Username + " " + data.Mppass + " " +
				data.Ip + " " + data.Port + " " + skinServer;
			return StartImpl( data, classicubeSkins, args, ref shouldExit );
		}
		
		public static bool Start( string args, ref bool shouldExit ) {
			return StartImpl( null, true, args, ref shouldExit );
		}
		
		static bool StartImpl( ClientStartData data, bool classicubeSkins,
		                      string args, ref bool shouldExit ) {
			Process process = null;
			string path = Path.Combine( AppDomain.CurrentDomain.BaseDirectory, "ClassicalSharp.exe" );
			if( !File.Exists( path ) )
				return false;
			
			CheckSettings( data, classicubeSkins, out shouldExit );
			if( Type.GetType( "Mono.Runtime" ) != null ) {
				process = Process.Start( "mono", "\"" + path + "\" " + args );
			} else {
				process = Process.Start( path, args );
			}
			return true;
		}
		
		internal static void CheckSettings( ClientStartData data, bool classiCubeSkins, out bool shouldExit ) {
			shouldExit = false;
			// Make sure if the client has changed some settings in the meantime, we keep the changes
			if( !Options.Load() )
				return;
			shouldExit = Options.GetBool( OptionsKey.AutoCloseLauncher, false );
			if( data == null ) return;
			
			Options.Set( "launcher-username", data.Username );
			Options.Set( "launcher-ip", data.Ip );
			Options.Set( "launcher-port", data.Port );
			Options.Set( "launcher-mppass", Secure.Encode( data.Mppass, data.Username ) );
			Options.Set( "launcher-ccskins", classiCubeSkins );		
			Options.Save();
		}
	}
}