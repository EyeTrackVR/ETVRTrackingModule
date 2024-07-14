# ETVR VRCFT Tracking Module 

ETVR Tracking mode is a VRCFT addon dedicated to EyetrackVR Project.
It acts as middle ground for translating OSC messages sent by EyetrackVR to a format understandable by VRCFT project. 

With it:
- ETVR can work with whichever game VRCFT supports. 
- Users don't have to worry about setting up a different set of params than those required by VRCFT
- ETVR get's to be forever(*) compatible

## How to use this: 

### Module installation
####  When it gets registered in VRCFT Module registry  
To make use of this module, checkout the module registry in VRCFT app and install it from there. 
Everything will be ready to go. 

##### For now: 

You'll have to download the provided DLL file [from releases pages](https://github.com/lorow/ETVRTrackingModule/releases)
Next, place it under `C:\Users\{your_user}\AppData\Roaming\VRCFaceTracking\CustomLibs\` directory.

If the directory doesn't exist, feel free to create it.
Don't forget to replace the `{your_user}` part with your pc's name. 

    # For example:
    
    from:
    
    `C:\Users\{your_user}\AppData\Roaming\VRCFaceTracking\CustomLibs\`
    
    to:
    
    `C:\Users\lorow\AppData\Roaming\VRCFaceTracking\CustomLibs\`

### App setup

You'll need to change the `Port` in `Settings` from `9000` to `8889`.

Settings will save automatically, but for them to take effect, you'll need to restart the app.

## What's supported right now / roadmap 

### What's currently supported: 

This a bit more technical section meant more to showcase what params sent out by ETVR are currently supported 
and translated by the module to VRCFT.

For V1 params sent out by ETVR with their defaults :

        { "RightEyeLidExpandedSqueeze", 1f },
        { "LeftEyeLidExpandedSqueeze", 1f },
        { "LeftEyeX", 0f },
        { "RightEyeX", 0f },
        { "EyesY", 0f },

For v2 params sent out by ETVR with their default values:

        { "EyeX", 0f },
        { "EyeY", 0f },
        { "EyeLid", 1f },
        
        { "EyeLeftX", 0f },
        { "EyeLeftY", 0f },
        { "EyeRightX", 0f },
        { "EyeRightY", 0f },
        { "EyeLidLeft", 1f },
        { "EyeLidRight", 1f },

Additionally to those params:

##### Eye widen / Squint emulation

Eye widen is supported by default in v2 params, for v1 we're emulating it. Every time eye openness reported by ETVR
reaches above certain threshold, we try and smoothly widen the eye a little bit. 

Squinting. For v2 it's defined as a separate parameter. For v1 it is done the same way as eye widen.

##### Eyebrows emulation  

Eyebrows emulation is now supported as well, for both v1 and v2(UE) parameters. It works by smoothly adjusting VRCFTs parameters after openness reaches a certain threshold. 

#### Squinting support for V2 parameters  
 
Implemented similarly to widen, we smoothly adjust proper expressions for v1 and v2 params when we detect eye openness has reached a very low threshold.

#### Other stuff
- [More dev-ish stuff] unified protocol for talking with ETVR
- Configuration for adjusting emulation thresholds for squinting / widen / eyebrows if need be and OSC Port in case of conflicts 

## How to contribute:
Pretty much the same way as with [EyetrackVR Project](https://github.com/EyeTrackVR/eyetrackvr). 

### How do I get it to compile? 

To get this to compile: 

- Clone this project with either git or zip download. 
- Download the source code zip from [VRCFT release page](https://github.com/benaclejames/VRCFaceTracking/releases) and extract it next to this project. 
- Open this project with the IDE of your choice and hit build, it should compile into a DLL file. 

### Dev faq: 

##### Why download source code from releases instead of cloning the project? 

To avoid a situation where the code you compile the module against is too new. 
I did that, it results in a pretty weird error.
Downloading the source code ensures that you have the same code base that was used to compile the production app

##### Do I need to copy the file to the directory mentioned above? 

No. It's already setup as a build event. 
Once compiled the file will be copied over to `$(USERPROFILE)\AppData\Roaming\VRCFaceTracking\CustomLibs\$(ProjectName).dll` 

This also means that if you try to compile while vrcft is running, building will error out on the copying step. This is expected. 
Close down VRCFT and try buildig again.

##### Does this compile under linux? Can I work on this from linux?

I am not sure. All of the development was done on a Windows machine. Feel free to try tho 

One step that you'll need to modify is the after build copy event in  `ETVRTrackingModule.csproj` , either completely remove it or replace the path with the correct one. 
