# fidobot
A Discord bot to clean 

# Usage
- `/eat` : Configure the current (or specified) thread to be deleted. If used on a forum, configures default behaviour for existing and/or future threads.
	-> Time Type : Choose between seconds, minutes, hours or days.
	-> Time Value : Specify in how many Time Type.
	-> Channel (Defaults to current if used in a thread) : Specify a thread or forum.
	-> Eat Existing (Forums only) : Yes or no choice to decide whether Fido should eat existing threads according to specified time options.
	-> Eat Future (Forums only) : Yes or no choice to decide whether Fido should be enabled for future threads created in this forum.
- `/donteat` : Disables Fido for the current (or specified) thread or forum. This command overrides forum configuration if fido is enabled on it.
	-> Channel (Defaults to current if used in a thread) : Specify a thread of forum.
