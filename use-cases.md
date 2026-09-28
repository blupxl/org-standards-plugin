# Use cases

**The goal: work that meets the company's standards the first time, so reviews can be about the
business logic instead of rules the developer couldn't have known.**

This document describes who that helps, what goes wrong for them today, and what changes. For how
it's built, see [design-notes.md](design-notes.md).

## The problem

- **Standards are scattered.** Brand colors live with the design team, timeouts and caching rules
  with the platform team, the website's footer and accessibility rules with marketing and legal.
  They sit in wikis, design files and people's heads.
- **They differ by product.** The public app's timeout isn't the internal tool's. The co-branded
  product has its own palette. Knowing the general rule isn't enough.
- **So work gets built on assumptions, and fixed later.** The developer guesses or uses a sensible
  default. Review catches it, or it doesn't. Either way, the work goes around again: that's churn.
- **AI assistants make it faster to get wrong.** They produce fluent, confident code that follows
  generic best practice, not *this* company's rules: invented colors, hard-coded server addresses,
  a 30-second timeout where the product needs 10.

## What changes

- **The rules arrive with the task.** When a developer starts work, Claude gets the standards that
  apply to *this* product and *this* kind of work, and only those.
- **Conflicts are already settled.** Where a product overrides a company rule, the developer sees
  the product's rule and what it replaced. Nobody has to work out which one wins.
- **Gaps are stated, not guessed.** If a team's standards are unavailable, or nothing covers what
  was asked, the answer says so. The assistant doesn't fill the gap with a confident guess.
- **The work is checked before anyone reviews it.** A separate reviewer, which can't change files,
  checks the result against every required rule and cites the file and line.
- **Nobody has to ask for any of this.** The repository declares that company standards apply. A
  developer's ordinary request ("add a settings page") is enough.

## Who it helps

### 1. A developer building a feature for a product

*Example: a catalog API for the XYZ Public App.*

- **Today:** the developer uses the timeout and caching approach they know. The product actually
  needs a 10-second timeout, not the company's usual 30, and catalog responses cached for five
  minutes. Review catches it, if the reviewer knows the product. Otherwise production does.
- **With the plugin:** the standards arrive with the product's rules already applied, each marked
  with what it replaced. The example code for the shared cache comes with them. The reviewer checks
  the finished code against each rule.
- **Benefit:** correct the first time, with fewer review round trips.

### 2. UI work and design conversations

*Example: a quick mockup to discuss a form with a UI/UX designer.*

- **Today:** a mockup gets whatever colors and controls look reasonable. The conversation with the
  designer starts with "that's not our blue" instead of the actual question.
- **With the plugin:** the page uses the company's color tokens and components from the start. The
  assistant lists what the standards *don't* cover (layout, units, date formats), which becomes the
  agenda for the conversation.
- **Benefit:** design reviews spend their time on design decisions, not on corrections.

### 3. The company's public website

*Example: a new "Generation portfolio" page under Our expertise.*

- **Today:** navigation order, button wording, the skip-to-content link, heading order and the
  footer's legal line are easy to get slightly wrong. They're caught late by marketing, legal or an
  accessibility review, and each catch is another round trip.
- **With the plugin:** the website's structure, voice, accessibility and footer rules are part of the
  task. In the demo run, the reviewer checked every required rule, each with a line number, and it
  passed.
- **Benefit:** fewer late-stage fixes, and accessibility and legal basics that aren't forgotten.

### 4. New developers and contractors

- **Today:** the standards are tribal knowledge. New people learn them by getting them wrong in
  review.
- **With the plugin:** the repository says which company, product and technology apply. A new
  developer's first request is handled to the same standards as a veteran's.
- **Benefit:** productive sooner, and consistent output across people and teams.

### 5. The teams that own the standards

*Example: the design team, the platform team.*

- **Today:** they write standards and then chase teams to follow them. They find out standards are
  missing only when someone happens to ask.
- **With the plugin:** they publish standards as short Markdown documents in their own area. A change
  reaches every project on the next run. When work touches something their standards don't cover,
  it shows up in the "Not covered" list.
- **Benefit:** standards that get used, not just written, and a list of missing standards grounded in
  real work.

### 6. Code reviewers and tech leads

- **Today:** a large part of review is checking conventions: colors, timeouts, naming, the footer.
  It's necessary, repetitive, and it crowds out attention to the logic.
- **With the plugin:** the rule-checking arrives with the work, as a report that cites each rule and
  the line it applies to.
- **Benefit:** human review focuses on what only a human can judge: the business logic and the
  design.

## Where the lift comes from

| Design choice | Effect on the work |
|---|---|
| Only the standards for this task, with details fetched when needed | The assistant follows what applies instead of skimming everything |
| Product overrides resolved before the assistant sees them | No guessing between conflicting rules |
| Unavailable or missing standards stated in the answer | No confident guesses where there's no rule |
| A separate, read-only reviewer | The author doesn't grade their own work |
| Standards kept as files owned by the teams that write them | Rules stay current and have an owner |
| The repository declares the scope | Standards apply without anyone having to remember to ask |

## What it doesn't do

- **It doesn't write the business logic or decide business rules.** It takes the conventions off the
  developer's mind so their attention goes there.
- **It doesn't replace human review.** It takes the rule-checking out of it.
- **It doesn't block a build.** The check runs when the work is done, not as a gate in the pipeline.
  That would be a natural next step.
- **It's a proof of concept.** The standards in it are placeholders that show the format. A real
  deployment starts from a company's actual standards.
