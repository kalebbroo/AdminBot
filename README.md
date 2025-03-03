# Discord Admin Bot

A powerful, feature-rich Discord bot that helps you manage your server, engage your community, and create an awesome Discord experience.

## 🌟 Features

### 🛡️ Moderation Tools
- Warning system
- Advanced logging
- Auto-moderation
- Role management
- Channel management

### 🎮 User Engagement
- XP and leveling system
- Custom events
- Voice features
- Server activities

### 🎙️ Voice Features
- AI Text-to-Speech
- Voice activity tracking
- Music commands (coming soon)

## 📝 Quick Start Guide

### Installation
1. Download the bot files
2. Copy `.env.example` to `.env`
3. Fill in your bot token and other settings in `.env`
4. Double-click `install.bat`
5. Wait for installation to complete

That's it! The bot will start automatically.

## 🤖 Bot Commands

### Moderation Commands

#### `/warn [user] [reason]`
Give a warning to a user
```
Example:
/warn @BadUser Spamming in general chat
```

#### `/warnings [user]`
View warnings for a user
```
Example:
/warnings @User
```

#### `/setup_rules`
Set up server rules
```
Opens an interactive menu where you can:
- Write your rules
- Format them nicely
- Add them to a specified channel
```

### XP System Commands

#### `/rank [user]`
Check your or another user's rank
```
Example:
/rank
/rank @Friend
Shows:
- Current level
- XP progress
- Rank on server
- Activity stats
```

#### `/leaderboard`
View server leaderboard
```
Shows top members by:
- XP gained
- Messages sent
- Voice activity
```

### Voice Commands

#### `/tts [text] [voice] [speed]`
Convert text to speech using AI voices
```
Example:
/tts Hello everyone! voice:alloy speed:1.0
Available voices:
- alloy (neutral)
- echo (male)
- fable (male)
- onyx (male)
- nova (female)
- shimmer (female)
```

## 🌟 Special Features

### XP System
Members earn XP through various activities:
- Sending messages (20+ characters)
- Voice chat activity
- Reactions and engagement
- Special events

#### XP Events
Admins can create special XP events:
```
/xp-event double_xp 24
Creates a 24-hour double XP event

/xp-event triple_xp 12
Creates a 12-hour triple XP event
```

### Level Roles
Members automatically receive roles as they level up:
1. Set up level roles in settings
2. Members get roles automatically
3. Optional announcements

Example setup:
```
Level 5 = "Active Member"
Level 10 = "Regular"
Level 20 = "Veteran"
Level 50 = "Elite"
```

### Warning System
Three-level warning system:
1. **Low**: Minor rule breaks
2. **Medium**: Serious issues
3. **High**: Major violations

Warnings can:
- Expire automatically
- Be pardoned by admins
- Trigger auto-actions

Example:
```
/warn @User Spamming in chat severity:1
Creates a low severity warning

/warn @User Inappropriate content severity:2
Creates a medium severity warning
```

## 📊 Stats Tracking

The bot tracks various stats:
- Messages sent
- Voice activity
- Reactions given
- Name changes
- Command usage

View stats with:
```
/profile @User
Shows detailed user statistics

/serverstats
Shows server-wide statistics
```

## ⚙️ Admin Controls

### Server Setup
Use the setup command for initial configuration:
```
/setup
Opens interactive setup menu for:
- Welcome messages
- Logging channels
- Role configuration
- XP settings
```

### Event Creation
Create server events easily:
```
/event create
Options:
- Duration
- XP multiplier
- Special rewards
- Target roles
```

### Custom Commands
Add custom commands:
```
/command add
Example:
Name: rules
Response: Here are our server rules...
```

## 🎨 Customization

### Welcome Messages
Customize new member welcomes:
```
/welcome setup
Options:
- Custom message
- Embed color
- Welcome image
- Auto roles
```

### Level Up Messages
Customize level up announcements:
```
/levelup setup
Options:
- Channel selection
- Message format
- Ping settings
- Role rewards
```

## 📈 Dashboard (Web Interface Coming Soon)

Future features will include:
- Web dashboard
- Advanced analytics
- Custom configurations
- Detailed statistics

## 🆘 Getting Help

Need help? Use these commands:
```
/help
Shows all commands

/help [command]
Shows detailed help for specific command

/support
Gets support information
```

## 🔧 Troubleshooting

Common issues and solutions:

### Bot Not Responding?
1. Check if bot is online
2. Verify bot has correct permissions
3. Check command usage in `/help`

### Commands Not Working?
1. Make sure bot has required permissions
2. Check command syntax in `/help`
3. Verify server settings

### XP Not Being Awarded?
1. Messages must be 20+ characters
2. Check for cooldown period
3. Verify channel isn't excluded

## 📋 Requirements

Bot needs these permissions:
- Manage Messages
- Manage Roles
- Voice Connect
- Voice Speak
- Send Messages
- View Channels
- Add Reactions

## 🔒 Privacy & Security

The bot:
- Only stores necessary data
- Never shares information
- Keeps logs for 30 days
- Can be configured for privacy

## 🔄 Updates

The bot auto-updates to include:
- New features
- Security updates
- Bug fixes
- Performance improvements

## 💝 Support the Project

Ways to help:
1. Report bugs
2. Suggest features
3. Share with others
4. Star on GitHub

---

Remember to:
1. Keep your `.env` file secure
2. Regularly check logs
3. Update bot permissions as needed
4. Back up configurations

Need more help? Join our support server or open an issue on GitHub!