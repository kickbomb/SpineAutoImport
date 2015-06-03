# SpineAutoImport

A simple Unity extension that automates the otherwise-tedious process of importing a Spine rig into a project that uses 2D Toolkit.

# Installation

Create a folder called **SpineAutoImport** in your Unity project's **Assets** folder, and copy the contents of this repository into it.

# Quick Start

If you're already very familiar with Spine, this quick start may be all you need!

1. Set up your default sprite collection settings in Unity's Preferences menu, on the **Spine Import** page.
2. Create a folder in your Unity project which will contain all the assets relating to this Spine rig.
3. Export the skeleton from Spine, as JSON, into this folder.
4. Copy the images you used as attachments in Spine, into this folder.
5. Right-click the folder in Unity and choose **Spine->Import Folder (tk2d)**.
6. Appreciate the magic!

Prefer a video walkthrough? [Click here](TBD)!

# Tips

* The importer searches all sub-folders of your **destination folder**, so you can organize that however you'd like. Personally, I like to keep all the exports from Spine in a **Source** folder, so only the imported game-ready assets sit at the root.
* You can import multiple skeletons at once. If the **destination folder** contains more than one skeleton JSON, each one will generate its own tk2dSpriteCollection and SkeletonDataAsset.
* You can import multiple folders at once by selecting them all, then right-clicking any one and choosing **Spine->Import Folder (tk2d)**.
* Any images present in the **destination folder** which are not referenced by any imported skeleton will be automatically cleaned up. (Note that the images are *deleted*, so make sure you're importing copies, not your originals!)

# Known Issues

If you re-run the importer on a folder which already contains a tk2dSpriteCollection and/or SkeletonDataAsset, those assets will be *replaced* and will get new asset GUIDs assigned. This will break existing links to those assets. In most cases, however, this is easy to avoid:

* If you update the Spine rig, simply re-export the JSON file over top of the existing one in your Unity project. There's no need to re-run the importer.
* If you update any of the images attached to the rig, simply replace those images in your Unity project (or update them directly). Provided you only *changed* images (and did not *add* or *remove* any images), there's no need to re-run the importer.

If you update the rig and stop using some images, you can safely ignore them. They'll still hang out in your project and be present in the sprite collection, they just won't be referenced. If you really need to squeeze down your build size, you can manually remove the unused images from the sprite collection and from the project with no ill effects.

If you update the rig and add new images, you can manually add the new images to the existing sprite collection with no ill effects.

I know this kind of sucks for certain workflows. I'll get around to fixing it at some point... or you could always submit a pull request. ;)

# Detailed Usage

If you have problems with the quick start, this section presents the same workflow in much more detail.

You should start by opening Unity's Preferences menu and setting up your default sprite collection settings on the **Spine Import** page. When the importer creates new sprite collections, they'll be created with these settings. You should only need to set this up once, since you'll generally want all sprite collections in your project to use the same settings.

With that done, and assuming you've already created a rig in [Spine](http://esotericsoftware.com), the first step to bringing it into Unity is to export the skeleton and its dependent image files into your Unity project. Create a **destination folder** in your project which will contain all the assets relating to this Spine rig. (When we're done, this folder will contain the skeleton and images exported from Spine as well as the imported SkeletonDataAsset and tk2dSpriteCollection to be used in Unity.)

Now that you have a **destination folder**, export the skeleton into it:

1. In the Spine editor, click the Spine logo in the top-left to open the main menu, then choose **Export**.
2. In the column on the left, under the **Data** heading, choose **JSON**.
3. In the **Export JSON** settings on the right, set the **Output folder** to your **destination folder** or any sub-folder inside it.
4. Set **Extension** to **.json**.
5. Set **Format** to **JSON**.
6. You can tick **Non-essential data** and/or **Pretty print** if you want a more human-readable export for debugging, but the importer will also work fine with one or both of these disabled.
7. Leave **Create atlas** *un-checked*.
8. Click the **Export** button at the bottom of the window.

Finally, you'll also need to copy over all the image files you used as attachments in Spine. Those images need to reside in your **destination folder** or any sub-folder inside it.

Now you should have something like this:

	Assets/
		YourDestinationFolder/
			skeleton.json
			image1.png
			image2.png
			image3.png
			...

Or if you're like me you might choose to organize all the exported stuff into sub-folders, which can make for a cleaner Unity project view:

	Assets/
		YourDestinationFolder/
			Source/
				skeleton.json
				images/
					image1.png
					image2.png
					image3.png
					...

It doesn't really matter, because the importer will search all sub-folders of your **destination folder** and automatically hook up everything it finds. And speaking of importing, let's do that now:

1. In Unity, right-click your **destination folder** and choose **Spine->Import Folder (tk2d)**.
2. Wait a moment. If you're importing a very complex skeleton and/or a lot of images, Unity may appear to stall; just give it a minute, and it should finish up just fine. (Maybe one day I'll put this behind a nice progress bar or something.)
3. Appreciate the magic!

You should now have a few new assets in your **destination folder**:

	Assets/
		YourDestinationFolder/
			YourDestinationFolder Atlas Data/ (folder)
			YourDestinationFolder Atlas (tk2dSpriteCollection)
			YourDestinationFolder SkeletonData (SkeletonDataAsset)

Note that the tk2dSpriteCollection and SkeletonDataAsset have been named after the folder you placed them in. Usually you'll name the folder after the character (or whatever else) this Spine rig represents, so in practice you might see something more like this:

	Assets/
		Player/
			Player Atlas Data/
			Player Atlas
			Player SkeletonData

To add this rig to your scene, right-click the generated SkeletonData asset and choose **Spine -> Spawn**. And you're done!

# License

Copyright © 2015 Kickbomb Entertainment LLC.

You may use, copy, modify, and/or distribute this software and associated documentation (the "Software") free of charge, and permit persons to whom the Software is furnished to do so, provided this permission notice is included in all copies or substantial portions of the Software. You may not sell or otherwise profit from the Software, except that if you use the Software in the process of creating a game or application, you may sell or otherwise profit from the game or application.

THIS SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THIS SOFTWARE OR THE USE OR OTHER DEALINGS IN THIS SOFTWARE.
