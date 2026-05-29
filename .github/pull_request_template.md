## General Development Checklist

- [ ] Is the use case implemented via Clean Architecture?
- [ ] Is the use case implemented with a feature toggle?
- [ ] Is the use case initiated via a Domain Service (`I<Service>Service`)?
- [ ] Does the Domain Service document what exceptions it may throw?
- [ ] Does the Use Case validate it's input parameter(s)?
- [ ] Does the Use Case take a Command or Query as it's parameter?
- [ ] Does the Use Case's Domain Service return a Dto/Model and not an Entity?
- [ ] Does the use case log what it's doing?
- [ ] Does the use case record metrics to track its usage and performance?
	- [ ] Usage
	- [ ] Performance
	- [ ] Alerts
	- [ ] Notifications
- [ ] Does the use case publish an event with minimal details?
- [ ] Does the use case have automated test(s) that verify it?
- [ ] Is the use case announced in a change log?
- [ ] Is there a health check related to the use case that can be polled?
- [ ] Is the use case multilingual?
- [ ] Is the use case included in a trace?
- [ ] Is the use case protected with Authentication?
- [ ] Is the use case protected with Authorization?
- [ ] Is the use case idempotent?
- [ ] Is there any alerts that need to be triggered for any of the usage scenarios?
	- [ ] Any notifications that need to be sent?

## API Development Checklist
- [ ] Are all dependencies injected via Asp.Net DI?
- [ ] Are all routes defined per the [Standard Route Patterns](onenote:Minimal%20APIs.one#Route%20Templates%20%20Patterns&section-id={CDEE1127-2365-4EF0-A3E8-895D1C9282B6}&page-id={DED95261-2A9C-402A-91D5-E6989BBE5F46}&end&base-path=https://d.docs.live.net/21299d8254c6b649/OneNote/Wes's%20Notebook/03_Resources)?
- [ ] Do all route parameters specify proper route constraints?
- [ ] Does the API method enforce API Client Authentication if required?
- [ ] Does the API ensure that the API Client has Authorization to retrieve and/or manipulate the Entity that it's operating on?
- [ ] Does the API only return Models/Dtos and not Entity classes?
- [ ] Does the API properly support [Searching, Filtering, Paging & Sorting](onenote:Minimal%20APIs.one#Request%20\%20Response%20Modelling&section-id={CDEE1127-2365-4EF0-A3E8-895D1C9282B6}&page-id={54A341C6-8BD8-4ED8-9380-49BC969D86E2}&end&base-path=https://d.docs.live.net/21299d8254c6b649/OneNote/Wes's%20Notebook/03_Resources)?
- [ ] Does the API return proper [Status Codes and TypedResponse Types](onenote:Minimal%20APIs.one#Status%20Codes%20%20Responses&section-id={CDEE1127-2365-4EF0-A3E8-895D1C9282B6}&page-id={DD1ED733-5DCF-4902-B8DA-F3EA56B6462E}&end&base-path=https://d.docs.live.net/21299d8254c6b649/OneNote/Wes's%20Notebook/03_Resources)?

## Web UI Development Checklist
- [ ] Are publicly accessible forms protected by CAPTCHA to prevent bot submissions?

## Mobile UI Development Checklist



## CLI Development Checklist
