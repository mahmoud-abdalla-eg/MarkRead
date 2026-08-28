export const SAMPLE_MARKDOWN = `# Antispam Case Review: AI Wrong Suggestions & Misrouting Analysis

**Date of Review:** 2026-08-28  
**Core Problem Solved:** The customer asks for help or how-to guidance, but the email ends up in Antispam, and the AI gives the support team an irrelevant, wrong suggestion.

---

## 1. The Oversight in the Initial Review

In the initial review, we focused too much on the mechanics of the new code without directly addressing the support team's biggest day-to-day pain point:

> **The Problem:**  
> A customer writes in asking: *"How do we configure DKIM?"* or *"Can we temporarily bypass SPF check?"*  
> Because the email contains trigger words like "SPF" or "DKIM", the dispatcher misroutes it into Antispam.  
> The AI assumes it is a delivery bounce, and suggests a draft asking the customer: *"Please provide the original .eml bounce report"*.  
> The support agent looks at the ticket and sees that the AI suggestion is completely off-topic and useless.

---

## 2. Root Cause Analysis

The incident was caused by three interconnected issues across the triage pipeline:

1. **Keyword Overweighting:** The classifier weighted technical vocabulary higher than conversational intent verbs.
2. **Missing Negative Constraints:** The Antispam prompt lacked rules preventing it from requesting bounce logs when no bounce was reported.
3. **Draft Confidence Scoring:** The suggestion engine had a threshold of 0.65, which allowed low-confidence classifications to surface.

| Metric | Before Fix | After Fix | Improvement |
| :--- | :--- | :--- | :--- |
| **Misrouting Rate** | 18.4% | 1.8% | -90.2% |
| **Agent Override Rate** | 34.1% | 4.2% | -87.7% |
| **First Contact Resolution** | 58.2% | 83.5% | +43.5% |

---

## 3. Configuration Fix Implementation

\`\`\`python
def classify_ticket_intent(subject: str, body: str) -> str:
    """
    Classifies incoming tickets based on semantic intent
    rather than isolated domain keywords.
    """
    intent_signals = {
        "how_to": ["how to", "how can i", "guide for", "steps to configure"],
        "antispam_bounce": ["rejected by server", "550 5.7.1", "delivery failed", "undeliverable"],
        "whitelist_request": ["whitelist domain", "allowlist ip", "unblock sender"]
    }
    
    normalized_text = f"{subject} {body}".lower()
    for category, patterns in intent_signals.items():
        if any(pattern in normalized_text for pattern in patterns):
            return category
            
    return "general_inquiry"
\`\`\`

---

## 4. Verification Checklist

- [x] Routing rules updated in production
- [x] Confidence threshold increased from 0.65 to 0.85
- [x] Regression testing across 500 historic tickets
- [ ] Agent training session scheduled for next week
- [ ] Monitor false-positive rates for 48 hours

> **Note on Safety:** Always verify outbound AI suggestions before hitting send on critical production tickets.
`;
