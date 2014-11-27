﻿/*
 * PROPRIETARY INFORMATION.  This software is proprietary to
 * Side Effects Software Inc., and is not to be reproduced,
 * transmitted, or disclosed in any way without written permission.
 *
 * Produced by:
 *      Side Effects Software Inc
 *		123 Front Street West, Suite 1401
 *		Toronto, Ontario
 *		Canada   M5J 2M2
 *		416-504-9876
 *
 * COMMENTS:
 * 		Contains HAPI_Host which is a singleton class that maintains the singleton Houdini scene and 
 * 		all access to the Houdini runtime.
 * 
 */

using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif // UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

public static partial class HoudiniHost
{
	public static bool isRuntimeInitialized()
	{
#if ( UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || ( UNITY_METRO && UNITY_EDITOR ) )
		if ( !HoudiniSetPath.prIsPathSet )
			return false;
		else
		{
			try
			{
				processStatusCode( (HAPI_Result) HAPI_IsInitialized() );
				return true;
			}
			catch
			{
				return false;
			}
		}
#else
		return false;
#endif // ( UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || ( UNITY_METRO && UNITY_EDITOR ) )
	}

	public static bool initialize()
	{
#if !( UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || ( UNITY_METRO && UNITY_EDITOR ) )
		return false;
#else
		// During the batch creation of our .unitypackage file we don't want to actually
		// initialize  We use this environment variable to inhibit initialization.
		string no_init = System.Environment.GetEnvironmentVariable( "HAPI_UNITY_NO_INIT" );
		if ( no_init != null )
			return false;

		if ( !prHoudiniSceneExists )
		{
			HAPI_Result status_code;
			try
			{
				string otls_path = getAllFoldersInPath( Application.dataPath + "/OTLs/Scanned" );
				string dsos_path = getAllFoldersInPath( Application.dataPath + "/DSOs" );

				// Check version match.
					
				int houdini_major 			= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_MAJOR );
				int houdini_minor 			= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_MINOR );
				int houdini_build 			= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_BUILD );
				int houdini_patch			= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_PATCH );
				int houdini_engine_major 	= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_ENGINE_MAJOR );
				int houdini_engine_minor 	= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_ENGINE_MINOR );
				int houdini_engine_api 		= getEnvInt( HAPI_EnvIntType.HAPI_ENVINT_VERSION_HOUDINI_ENGINE_API );
					
				Debug.Log(  "Running Houdini Engine Unity Plugin Version: " +
							HoudiniVersion.HOUDINI_ENGINE_MAJOR + "." + 
							HoudiniVersion.HOUDINI_ENGINE_MINOR +
							", API: " + HoudiniVersion.HOUDINI_ENGINE_API );
					
				Debug.Log(  "Linking Against Houdini Engine Version: " + 
							houdini_engine_major + "." + houdini_engine_minor +
							", API: " + houdini_engine_api );
					
				Debug.Log(  "Underlying Houdini Core Version: " + 
							houdini_major + "." + houdini_minor + "." + houdini_build
							+ ( houdini_patch > 0 ? "." + houdini_patch : "" ) );
					
				// Make sure we are linking against the expected Houdini Engine API version.
				// Note: We don't need to be so strict as to require the BUILD to match.
				if ( houdini_engine_major != HoudiniVersion.HOUDINI_ENGINE_MAJOR ||
						houdini_engine_minor != HoudiniVersion.HOUDINI_ENGINE_MINOR ||
						houdini_engine_api	  != HoudiniVersion.HOUDINI_ENGINE_API )
				{
					throw new HoudiniError( "Cannot link to Houdini Engine because of version mismatch." );
				}

				HAPI_CookOptions cook_options = new HAPI_CookOptions();
				cook_options.splitGeosByGroup = prSplitGeosByGroup;
				cook_options.maxVerticesPerPrimitive = HoudiniConstants.HAPI_MAX_VERTICES_PER_FACE;
				cook_options.refineCurveToLinear = HoudiniConstants.HAPI_CURVE_REFINE_TO_LINEAR;
				cook_options.curveRefineLOD = HoudiniConstants.HAPI_CURVE_LOD;

				// Initialize.
				status_code = HAPI_Initialize( otls_path, dsos_path, ref cook_options, true, -1 );
				if ( status_code != HAPI_Result.HAPI_RESULT_ALREADY_INITIALIZED )
					processStatusCode( status_code );
			}
			catch ( HoudiniError error )
			{
				Debug.LogError( error.ToString() );
				return false;
			}
			catch ( System.Exception error )
			{
				Debug.LogError( error.ToString() );
				return false;
			}

			prHoudiniSceneExists = true;
		}

		return true;
#endif // !( UNITY_STANDALONE_WIN || UNITY_STANDALONE_OSX || ( UNITY_METRO && UNITY_EDITOR ) )
	}
}
