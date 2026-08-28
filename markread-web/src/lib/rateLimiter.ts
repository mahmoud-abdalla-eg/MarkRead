/**
 * Token Bucket Burst Rate Limiter
 * Allows burst operations up to capacity, refills gradually,
 * and enforces a cooldown when tokens are completely exhausted.
 */
export class TokenBucketLimiter {
  private tokens: number;
  private maxTokens: number;
  private refillIntervalMs: number;
  private cooldownDurationSeconds: number;
  private lastRefillTimestamp: number;
  private cooldownExpiresTimestamp: number;

  constructor(
    maxTokens: number = 3,
    refillIntervalMs: number = 5000,
    cooldownDurationSeconds: number = 15
  ) {
    this.tokens = maxTokens;
    this.maxTokens = maxTokens;
    this.refillIntervalMs = refillIntervalMs;
    this.cooldownDurationSeconds = cooldownDurationSeconds;
    this.lastRefillTimestamp = Date.now();
    this.cooldownExpiresTimestamp = 0;
  }

  private refill(): void {
    const now = Date.now();

    // If currently in enforced cooldown
    if (now < this.cooldownExpiresTimestamp) {
      return;
    }

    const elapsed = now - this.lastRefillTimestamp;
    const tokensToAdd = Math.floor(elapsed / this.refillIntervalMs);

    if (tokensToAdd > 0) {
      this.tokens = Math.min(this.maxTokens, this.tokens + tokensToAdd);
      this.lastRefillTimestamp = now - (elapsed % this.refillIntervalMs);
    }
  }

  /**
   * Attempts to consume 1 token.
   * Returns { allowed: true } or { allowed: false, cooldownRemainingSeconds: N }
   */
  public tryConsume(): { allowed: boolean; cooldownRemainingSeconds: number } {
    const now = Date.now();

    // Still in cooldown?
    if (now < this.cooldownExpiresTimestamp) {
      const remaining = Math.ceil((this.cooldownExpiresTimestamp - now) / 1000);
      return { allowed: false, cooldownRemainingSeconds: remaining };
    }

    this.refill();

    if (this.tokens >= 1) {
      this.tokens -= 1;
      return { allowed: true, cooldownRemainingSeconds: 0 };
    }

    // Out of tokens: trigger cooldown
    this.cooldownExpiresTimestamp = now + this.cooldownDurationSeconds * 1000;
    this.tokens = 0;
    return { allowed: false, cooldownRemainingSeconds: this.cooldownDurationSeconds };
  }

  /**
   * Returns remaining cooldown in seconds (0 if ready).
   */
  public getCooldownSeconds(): number {
    const now = Date.now();
    if (now < this.cooldownExpiresTimestamp) {
      return Math.ceil((this.cooldownExpiresTimestamp - now) / 1000);
    }
    return 0;
  }

  public getAvailableTokens(): number {
    this.refill();
    return this.tokens;
  }
}

/**
 * Sliding Window Drop Throttler
 * Prevents rapid-fire automated file drop spam loops.
 */
export class DropThrottler {
  private timestamps: number[] = [];
  private maxDrops: number;
  private windowMs: number;

  constructor(maxDrops: number = 3, windowMs: number = 5000) {
    this.maxDrops = maxDrops;
    this.windowMs = windowMs;
  }

  public allowDrop(): boolean {
    const now = Date.now();
    this.timestamps = this.timestamps.filter((t) => now - t < this.windowMs);

    if (this.timestamps.length >= this.maxDrops) {
      return false;
    }

    this.timestamps.push(now);
    return true;
  }
}
