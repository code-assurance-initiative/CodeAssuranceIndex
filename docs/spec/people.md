# People and machine accounts in a repository's history

Every dimension page that reads a repository's history person by person (today the bus factor, D16, and knowledge
freshness, D34) counts people and machine accounts as this page defines them. A dimension page states only where it
differs.

## Scope

- **Whose commits:** the author of each commit read. The committer does not count.
- **Which commits:** the commits reachable from the analysed commit, newest first by commit date, up to the limit
  under Pending re-definition. Merge commits are read: they count as commits and their authors as people, but a merge
  gives no knowledge of files.
- **Dates:** every date is the commit date (when the commit landed), not the author date; the author decides only who
  made the commit.

## People

- **One person, several identities.** A person who commits under several names or email addresses counts as one
  person. Where the repository declares who is who (for example in a mailmap file), that declaration comes first;
  identities the history otherwise shows to belong to one person also count as one.
- **Different people stay different.** Two people count as two.
- **A platform's private address is a person.** A person committing through the private address a hosting platform
  gives them counts as a person.

How an engine decides that identities belong to one person is the engine's method (decision 0024 §2).

## Machine accounts

A machine account is an identity that commits for a tool or a service, not for a person. Whether a commit comes from
a machine account depends only on the author recorded on that commit; the repository's declaration of who is who does
not change it. There are two kinds:

- **Coding agents:** an AI coding agent committing for a person or a vendor, for example the coding agent a hosting
  platform itself provides, or an AI tool committing from its vendor's service address.
- **Other machine accounts:** for example accounts the hosting platform marks as bots or apps, dependency-update
  services, continuous-integration, build and release automation, and a project's own automation account.

The two kinds differ only under the automation-only file rule below. How an engine recognises a machine account, and
tells the two kinds apart, is the engine's method.

## How machine commits count

- **Not people.** Machine commits are left out of every repository-wide count of people: the number of distinct
  people, and every share of commits by people (a machine commit is in neither the numerator nor the total). Each
  dimension page defines the figures it builds from these.
- **Still commits.** The repository's number of commits read includes machine commits; a dimension page that sets a
  minimum number of commits, or reads how recently the repository was active, counts them.
- **Still knowledge of files.** A machine commit gives its account knowledge of the files it touches, as a person's
  commit does, and a machine account can hold the largest share of a file. A finding never names a machine account
  as a person at risk; this does not change the score.
- **Automation-only files.** A file is left out of a dimension's file population when at least one commit read touched
  it, every commit read that touched it came from a machine account that is not a coding agent, and at least one
  person authored some commit read in the repository. One commit by a person or a coding agent keeps the file in.

## Pending re-definition

These are parameters of the existing reading's model. Today's scores depend on them, and a later decision under
decision 0013 will replace each with a threshold on the property.

- **Commits read:** the 20,000 most recent commits. Older commits and their authors are not read.

## What this is not

- It is not verified identity. Joining identities into one person is a judgement from the history: two people may be
  counted as one, and one person whose identities the history does not link may be counted as several.
- An automation account that commits under an ordinary personal name and address counts as a person.
