// Copyright (c) 2026, MYT
//
// All rights reserved.

#include "gtest/gtest.h"

#include "wallet/wallet2.h"

namespace
{
  void expect_no_fast_refresh_checkpoint_jump(cryptonote::network_type nettype, uint64_t stop_height)
  {
    tools::wallet2 wallet(nettype, 1, true);
    epee::wipeable_string password;
    crypto::secret_key recovery_key = crypto::null_skey;
    recovery_key.data[0] = static_cast<unsigned char>(nettype + 1);

    wallet.generate("", password, recovery_key, true, false);
    ASSERT_EQ(1, wallet.get_blockchain_current_height());
    ASSERT_TRUE(wallet.init("http://127.0.0.1:1"));

    uint64_t blocks_fetched = 0;
    EXPECT_ANY_THROW(wallet.refresh(true, stop_height, blocks_fetched));
    EXPECT_EQ(1, wallet.get_blockchain_current_height());
    wallet.stop();
  }
}

TEST(wallet_default_checkpoints, testnet_fast_refresh_cannot_select_removed_checkpoints)
{
  for (const uint64_t stop_height : {1000001ULL, 1058601ULL, 1450001ULL})
    expect_no_fast_refresh_checkpoint_jump(cryptonote::TESTNET, stop_height);
}

TEST(wallet_default_checkpoints, stagenet_fast_refresh_cannot_select_removed_checkpoints)
{
  for (const uint64_t stop_height : {10001ULL, 550001ULL})
    expect_no_fast_refresh_checkpoint_jump(cryptonote::STAGENET, stop_height);
}
