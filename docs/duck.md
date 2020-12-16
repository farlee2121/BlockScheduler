
- need to be able to add and remove blocks
- separately need to be able to read configuration for what to block
- need an engine that checks current blocks against current scheduled blocks
- What is my mechanism for retaining blocks until original scheduled release? -> worry about it later
// config save format url days starttime end time, associate multiple records if we need to block multiple time spans in a day or a different time range per day


What is left to be done for mvp?
- scheduling
- configuration definition

How do I handle scheduling? What are my needs
 - Requirement: I should be able to schedule a block to start or end at any time of day, to the resolution of a minute
 - Requirement: I should be able to schedule a block to happen on one or more days
 - NOT Requirement: I don't need to handle schedules at a recurrence of longer than a week (i.e. monthly, every two weeks, etc)
 - NOT Requirement: I don't need to handle multiple segments in a day. That can be achieved with multiple blocks for now
 - Requirement?: I would like the blocker to periodically check that blocks have not been removed
 - V1.1 Requirement: I would like for the blocker to keep a site blocked until its original scheduled end time, even if the configuration is changed


What does this mean for my api?
- well, If I simply run every minute, then I only need one method UpdateBlockedSites
- If I would like to run more sparingly, then I need some kind of scheduling service. 
- For now I feel like simple is better, simply run the command every minute and adapt if needed
  - I still need to add ability to parse a config and compare current blocks against scheduled blocks

Scheduling Tech options
 - windows scheduler
    - leverages built-in scheduler
 - windows service with cron scheduler
    - allows me to utilize features like file watches to prevent block tampering
