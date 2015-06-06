using UnityEditor;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using tk2dEditor.SpriteCollectionEditor;
using Spine;

namespace Kickbomb
{

public class SpineAutoImport
{
	// --------------------------------------------------------------------------------------------
	public static List<T> GetAssetsRecursively<T>(string path)
		where T: Object
	{
		List<T> assets = new List<T>();
		foreach(string filePath in System.IO.Directory.GetFiles(path))
		{
			string adjustedPath = filePath.Replace('\\', '/');
			System.IO.FileInfo fileInfo = new System.IO.FileInfo(adjustedPath);
			if(fileInfo.Extension.ToLower() == ".meta")
				continue;

			T asset = AssetDatabase.LoadAssetAtPath(adjustedPath, typeof(T)) as T;
			if(asset != null) assets.Add(asset);
		}
		foreach(string folderPath in System.IO.Directory.GetDirectories(path))
		{
			assets.AddRange(GetAssetsRecursively<T>(folderPath.Replace('\\', '/')));
		}

		return assets;
	}

	// --------------------------------------------------------------------------------------------
    [MenuItem("Assets/Spine/Import Folder (tk2d)")]
    static void Import_tk2d()
    {
    	List<SkeletonDataAsset> results = new List<SkeletonDataAsset>();

    	List<Object> folders = new List<Object>();
    	foreach(Object selection in Selection.objects)
    	{
    		if(Directory.Exists(AssetDatabase.GetAssetPath(selection.GetInstanceID())))
    			folders.Add(selection);
    	}

    	foreach(Object selection in folders)
    	{
	    	// Auto figure the base path
	    	string path = "Assets/";
	    	string assetDirectory = AssetDatabase.GetAssetPath(selection.GetInstanceID());
	    	if(!System.String.IsNullOrEmpty(assetDirectory) && Directory.Exists(assetDirectory))
	    		path = assetDirectory + "/";
	    	string assetName = new DirectoryInfo(path).Name;

	    	List<TextAsset> textAssets = GetAssetsRecursively<TextAsset>(path);
	    	if(textAssets.Count != 1)
	    	{
	    		Debug.LogError(System.String.Format("Selection must contain exactly 1 skeleton json file (this selection contains {0})", textAssets.Count));
	    		return;
	    	}

	    	List<Texture2D> textures = GetAssetsRecursively<Texture2D>(path);
	    	if(textures.Count < 1)
	    	{
	    		Debug.LogError("Selection doesn't contain any textures");
	    	}

	    	// Find or create the SpriteCollection
	    	string atlasPath = Path.Combine(path, assetName + " Atlas" + ".prefab");
	    	tk2dSpriteCollection atlas = AssetDatabase.LoadAssetAtPath(atlasPath, typeof(tk2dSpriteCollection)) as tk2dSpriteCollection;
	    	if(atlas == null)
	    	{
				atlas = tk2dSpriteCollectionEditor.CreateSpriteCollection(path, assetName + " Atlas");
		    	if(atlas == null)
		    	{
		    		Debug.LogError("Failed creating SpriteCollection");
		    		return;
		    	}
	    	}
	    	else if(atlas.spriteCollection != null)
	    	{
	    		// Remove existing SpriteCollectionData
	    		foreach(Material atlasMaterial in atlas.spriteCollection.materials)
	    		{
		    		// Remove the old atlas texture
		    		Texture atlasTexture = atlasMaterial.mainTexture;
		    		for(int i = 0; i < textures.Count; i++)
		    		{
		    			if(textures[i] == atlasTexture)
		    			{
		    				textures.RemoveAt(i);
		    				break;
		    			}
		    		}
		    		AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(atlasTexture));

		    		// Remove the old atlas material
		    		AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(atlasMaterial));
	    		}

	    		AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(atlas.spriteCollection));
	    		atlas.spriteCollection = null;

	    		AssetDatabase.Refresh();
	    	}

	    	// Apply settings
	    	atlas.allowMultipleAtlases = EditorPrefs.GetBool("SpineAutoImport.AllowMultipleAtlases", true);
			atlas.sizeDef.type = (tk2dSpriteCollectionSize.Type)EditorPrefs.GetInt("SpineAutoImport.Size", (int)tk2dSpriteCollectionSize.Type.Explicit);
	    	if(atlas.sizeDef.type == tk2dSpriteCollectionSize.Type.Explicit)
	    	{
		    	atlas.sizeDef.orthoSize = EditorPrefs.GetFloat("SpineAutoImport.OrthoSize", 10.0f);
		    	atlas.sizeDef.height = EditorPrefs.GetInt("SpineAutoImport.TargetHeight", 640);
	    	}
	    	else if(atlas.sizeDef.type == tk2dSpriteCollectionSize.Type.PixelsPerMeter)
	    	{
				atlas.sizeDef.pixelsPerMeter = EditorPrefs.GetFloat("SpineAutoImport.PixelsPerMeter", 100);
	    	}

	    	// Verify textures, and remove unreferenced ones
	    	{
	    		// Build a list of found texture names for comparison
	    		Dictionary<string, Texture2D> foundTextures = new Dictionary<string, Texture2D>(textures.Count);
		    	for(int i = 0; i < textures.Count; i++)
		    		foundTextures.Add(Path.GetFileNameWithoutExtension(textures[i].name), textures[i]);

				// We'll build up our final list of used textures in this dictionary
				// Key = slot name, Value = list of texture names bound to the slot (directly or in any skin)
				Dictionary<string, List<string>> attachments = new Dictionary<string, List<string>>();

		    	// Prep our JSON reader
		    	StringReader reader = new StringReader(textAssets[0].text);
		    	Dictionary<string, object> json = Json.Deserialize(reader) as Dictionary<string, object>;

				// Check slot definitions for textures which are directly assigned
				if(json.ContainsKey("slots"))
				{
		    		List<object> slots = json["slots"] as List<object>;
		    		for(int i = 0; i < slots.Count; i++)
					{
		    			Dictionary<string, object> slotData = slots[i] as Dictionary<string, object>;
		    			string slotName = slotData["name"] as string;

						// Create texture list for this slot if it hasn't been created already
						if(!attachments.ContainsKey(slotName))
							attachments.Add(slotName, new List<string>());

		    			// If a texture is directly bound, add it
						List<string> slotTextures = attachments[slotName];
		    			if(slotData.ContainsKey("attachment") && !slotTextures.Contains(slotData["attachment"] as string))
		    				slotTextures.Add(slotData["attachment"] as string);
					}
				}

				// Check skin definitions for textures which are indirectly assigned
				if(json.ContainsKey("skins"))
				{
					Dictionary<string, object> skins = json["skins"] as Dictionary<string, object>;
					foreach(KeyValuePair<string, object> skin in skins)
					{
						Dictionary<string, object> slotAssignments = skin.Value as Dictionary<string, object>;
						foreach(KeyValuePair<string, object> slotAssignment in slotAssignments)
						{
							string slotName = slotAssignment.Key;
							Dictionary<string, object> slotTextures = slotAssignment.Value as Dictionary<string, object>;
							foreach(KeyValuePair<string, object> slotTexture in slotTextures)
							{
								string textureName = slotTexture.Key;
								if(!attachments.ContainsKey(slotName))
									attachments.Add(slotName, new List<string>());
								if(!attachments[slotName].Contains(textureName))
									attachments[slotName].Add(textureName);
							}
						}
					}
				}

		    	// Build the final list of referenced texture names for comparison
		    	List<string> spriteNames = new List<string>();
		    	foreach(KeyValuePair<string, List<string>> attachment in attachments)
		    	{
		    		for(int i = 0; i < attachment.Value.Count; i++)
		    		{
		    			if(!spriteNames.Contains(attachment.Value[i]))
		    				spriteNames.Add(attachment.Value[i]);
		    		}
		    	}

		    	// Warn about sprite references with missing textures
		    	bool missingTextures = false;
		    	for(int i = 0; i < spriteNames.Count; i++)
		    	{
		    		if(!foundTextures.ContainsKey(spriteNames[i]))
		    		{
		    			Debug.LogError(System.String.Format(
		    				"{0} references sprite \"{1}\" but no texture was found with that name",
		    				textAssets[0].name, spriteNames[i]
		    			));
		    			missingTextures = true;
		    		}
		    	}

		    	if(missingTextures) return;

		    	// Clean up unreferenced textures
		    	foreach(KeyValuePair<string, Texture2D> entry in foundTextures)
		    	{
		    		if(!spriteNames.Contains(entry.Key))
		    		{
		    			Debug.LogWarning("Removed unreferenced texture " + entry.Key);
		    			textures.Remove(entry.Value);
		    			AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(entry.Value));
		    		}
		    	}
		    }

	    	// Add textures
	    	SpriteCollectionProxy atlasProxy = new SpriteCollectionProxy(atlas);
	    	atlasProxy.textureParams.Clear();
	    	for(int i = 0; i < textures.Count; i++)
	    	{
	    		string name = atlasProxy.FindUniqueTextureName(textures[i].name);
	    		int slot = atlasProxy.FindOrCreateEmptySpriteSlot();
	    		atlasProxy.textureParams[slot].name = name;
	    		atlasProxy.textureParams[slot].colliderType = tk2dSpriteCollectionDefinition.ColliderType.UserDefined;
	    		atlasProxy.textureParams[slot].texture = textures[i];
	    	}

	    	// Commit the SpriteCollection
	    	atlasProxy.DeleteUnusedData();
	    	atlasProxy.CopyToTarget();
	 		tk2dSpriteCollectionBuilder.ResetCurrentBuild();
			if(!tk2dSpriteCollectionBuilder.Rebuild(atlas))
			{
				EditorUtility.DisplayDialog("Failed to commit sprite collection",
					"Please check the console for more details.", "Ok");
			}

			// Find or create the SkeletonData
			string skeletonPath = Path.Combine(path, assetName + " SkeletonData.asset");
			SkeletonDataAsset skeleton = AssetDatabase.LoadAssetAtPath(skeletonPath, typeof(SkeletonDataAsset)) as SkeletonDataAsset;
			if(skeleton == null)
			{
				skeleton = ScriptableObject.CreateInstance<SkeletonDataAsset>();
				AssetDatabase.CreateAsset(skeleton, skeletonPath);
				AssetDatabase.SaveAssets();
				skeleton = AssetDatabase.LoadAssetAtPath(skeletonPath, typeof(SkeletonDataAsset)) as SkeletonDataAsset;

				if(skeleton == null)
				{
					Debug.LogError("Failed creating SkeletonDataAsset");
					return;
				}
			}

			// Connect SkeletonData with json and textures
			skeleton.skeletonJSON = textAssets[0];
			skeleton.spriteCollection = atlas.spriteCollection;
			skeleton.defaultMix = 0.2f;
			skeleton.fromAnimation = new string[0];
			skeleton.toAnimation = new string[0];
			skeleton.duration = new float[0];
			skeleton.Reset();

			// Make sure everything gets persisted
			EditorUtility.SetDirty(atlas);
			EditorUtility.SetDirty(skeleton);

			results.Add(skeleton);
    	}

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		// Select the resulting SkeletonDataAssets
		Selection.objects = results.ToArray();
    }

	// --------------------------------------------------------------------------------------------
	[PreferenceItem("Spine Import")]
	static void PreferencesGUI()
	{
		// Defaults are the same as the tk2dSpriteCollection defaults
		tk2dSpriteCollectionSize.Type atlasType = (tk2dSpriteCollectionSize.Type)EditorPrefs.GetInt("SpineAutoImport.Size", (int)tk2dSpriteCollectionSize.Type.Explicit);
		bool multiAtlas = EditorPrefs.GetBool("SpineAutoImport.AllowMultipleAtlases", true);
		float orthoSize = EditorPrefs.GetFloat("SpineAutoImport.OrthoSize", 10.0f);
		int targetHeight = EditorPrefs.GetInt("SpineAutoImport.TargetHeight", 640);
		float pixelsPerMeter = EditorPrefs.GetFloat("SpineAutoImport.PixelsPerMeter", 100);

		// UI
		EditorGUILayout.HelpBox(
			"These settings will be applied to new 2D Toolkit sprite collections created during the import process.",
			MessageType.Info
		);
		multiAtlas = EditorGUILayout.Toggle("Multiple Atlases", multiAtlas);
		atlasType = (tk2dSpriteCollectionSize.Type)EditorGUILayout.EnumPopup("Size", atlasType);
		if(atlasType == tk2dSpriteCollectionSize.Type.Explicit)
		{
			orthoSize = EditorGUILayout.FloatField("Ortho Size", orthoSize);
			targetHeight = EditorGUILayout.IntField("Target Height", targetHeight);
		}
		else if(atlasType == tk2dSpriteCollectionSize.Type.PixelsPerMeter)
		{
			pixelsPerMeter = EditorGUILayout.FloatField("Pixels Per Meter", pixelsPerMeter);
		}

		// Apply changes
		if(GUI.changed)
		{
			EditorPrefs.SetBool("SpineAutoImport.AllowMultipleAtlases", multiAtlas);
			EditorPrefs.SetInt("SpineAutoImport.Size", (int)atlasType);
			EditorPrefs.SetFloat("SpineAutoImport.OrthoSize", orthoSize);
			EditorPrefs.SetInt("SpineAutoImport.TargetHeight", targetHeight);
			EditorPrefs.SetFloat("SpineAutoImport.PixelsPerMeter", pixelsPerMeter);
		}
	}

	// --------------------------------------------------------------------------------------------
    [MenuItem("Assets/Spine/Import Folder (tk2d)", true)]
    static bool ValidateImport_tk2d()
    {
    	if(Selection.activeObject == null) return false;
		if(!AssetDatabase.IsValidFolder(AssetDatabase.GetAssetPath(Selection.activeObject.GetInstanceID()))) return false;

    	return true;
    }
}

} // namespace
