Feature: Item Retrieval

Background:
    Given the client is configured to use host 'mock_host' and token 'jwt_token'
    And item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
        """
        {
            "id": "wepiqdxdzncjtnvmv5fegud4qy",
            "title": "Test Login",
            "tags": [
                "LastPass Import 9-19-20"
            ],
            "version": 3,
            "vault": {
                "id": "hfnjvi6aymbsnfc2xeeoheizda"
            },
            "category": "LOGIN",
            "lastEditedBy": "last edited id",
            "sections": [
                {
                    "id": "section id"
                },
                {
                    "id": "linked items",
                    "label": "Related Items"
                }
            ],
            "fields": [
                {
                    "id": "field id",
                    "type": "STRING",
                    "purpose": "purpose",
                    "label": "label",
                    "value": "value"
                }
            ],
            "urls": [
                {
                    "primary": true,
                    "href": "https://www.google.com"
                }
            ]
        }
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
        """
        [
            {
                "id": "wepiqdxdzncjtnvmv5fegud4qy",
                "title": "Test Login",
                "tags": [
                    "LastPass Import 9-19-20"
                ],
                "version": 3,
                "vault": {
                    "id": "hfnjvi6aymbsnfc2xeeoheizda"
                },
                "category": "LOGIN",
                "lastEditedBy": "last edited id",
                "urls": [
                    {
                        "primary": true,
                        "href": "https://www.google.com/"
                    }
                ]
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
        """
        [
            {
                "id": "wepiqdxdzncjtnvmv5fegud4qy",
                "title": "Test Login",
                "tags": [
                    "LastPass Import 9-19-20"
                ],
                "version": 3,
                "vault": {
                    "id": "hfnjvi6aymbsnfc2xeeoheizda"
                },
                "category": "LOGIN",
                "lastEditedBy": "last edited id",
                "urls": [
                    {
                        "primary": true,
                        "href": "https://www.google.com/"
                    }
                ]
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults/?filter=title eq "Automation"'
        """
        [
            {
                "id": "hfnjvi6aymbsnfc2xeeoheizda",
                "name": "Automation",
                "attributeVersion": 1,
                "contentVersion": 14,
                "items": 3,
                "type": "USER_CREATED",
                "createdAt": "2022-01-21T19:16:59Z",
                "updatedAt": "2022-01-22T20:30:13Z"
            }
        ]
        """
    And the server returns the following body for call to url 'mock_host/v1/vaults'
        """
        [
            {
                "id": "hfnjvi6aymbsnfc2xeeoheizda",
                "name": "Automation",
                "attributeVersion": 1,
                "contentVersion": 14,
                "items": 3,
                "type": "USER_CREATED",
                "createdAt": "2022-01-21T19:16:59Z",
                "updatedAt": "2022-01-22T20:30:13Z"
            }
        ]
        """

Scenario: Get item by ID and vault ID
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by title and vault ID
    When the user requests item with title 'Test Login' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by ID and vault title
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with title 'Automation'
    Then the following url should be called 'mock_host/v1/vaults?filter=title eq "Automation"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
        And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by title and vault title
    When the user requests item with title 'Test Login' in vault with title 'Automation'
    Then the following url should be called 'mock_host/v1/vaults?filter=title eq "Automation"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Getting all items in a vault by vault ID
    When the user requests all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
    And the client should return all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'

Scenario: Getting all items in a vault by vault title
    When the user requests all items in vault with title 'Automation'
    Then the following url should be called 'mock_host/v1/vaults?filter=title eq "Automation"'
    And the following url should be called 'mock_host/v1/vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
    And the client should return all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'

Scenario: Getting all vaults
    When the user requests all vaults
    Then the following url should be called 'mock_host/v1/vaults'
    And the client should return all vaults
