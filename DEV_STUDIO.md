# DEV_STUDIO.md

This repository uses Dev Studio with the **3-agent runtime model**.

## Active runtime roles

- orchestrator
- builder
- validator

## Workflow

planning → backlog → spec → doing → review → done

## Stage ownership

- orchestrator: planning → backlog
- builder: backlog → spec
- builder: spec → doing
- validator: doing → review
- validator: review → done

## Rules

1. Planning is durable and may remain in place after promotion
2. Backlog cards should be actionable and bounded
3. Epic cards should be split into child cards before implementation
4. Only one card should be advanced per controller cycle
5. Locks prevent collisions
6. Numbered filenames determine order when priorities are otherwise equal
7. Use feature prefixes (e.g., 'auth-login-form.md') to group related work
