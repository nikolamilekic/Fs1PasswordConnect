Feature: Item Retrieval

Background:
    Given item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
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
    And the server returns the following body for call to url 'vaults/hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
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
    And the server returns the following body for call to url 'vaults/hfnjvi6aymbsnfc2xeeoheizda/items'
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
    And the server returns the following body for call to url 'vaults/?filter=title eq "Automation"'
        """
        [
            {
                "id": "hfnjvi6aymbsnfc2xeeoheizda",
                "name": "Automation",
                "attributeVersion": 1,
                "contentVersion": 14,
                "items": 3,
                "type": "USER_CREATED",
                "createdAt": 1/21/2022 19:16:59,
                "updatedAt": 1/22/2022 20:30:13
            }
        ]
        """
    And the client is configured to use host 'mock_host' and token 'jwt_token'

Scenario: Get item by ID and vault ID
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by title and vault ID
    When the user requests item with title 'Test Login' in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
    And the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by ID and vault title
    When the user requests item with ID 'wepiqdxdzncjtnvmv5fegud4qy' in vault with title 'Automation'
    Then the following server endpoint should be called 'vaults?filter=title eq "Automation"'
    And the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Get item by title and vault title
    When the user requests item with title 'Test Login' in vault with title 'Automation'
    Then the following server endpoint should be called 'vaults?filter=title eq "Automation"'
    And the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items?filter=title eq "Test Login"'
    And the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items/wepiqdxdzncjtnvmv5fegud4qy'
    And the client should return item with ID 'wepiqdxdzncjtnvmv5fegud4qy'

Scenario: Getting all items in a vault by vault ID
    When the user requests all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
    Then the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items'
    And the client should return all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'

Scenario: Getting all items in a vault by vault title
    When the user requests all items in vault with title 'Automation'
    Then the following server endpoint should be called 'vaults?filter=title eq "Automation"'
    And the following server endpoint should be called 'hfnjvi6aymbsnfc2xeeoheizda/items'
    And the client should return all items in vault with ID 'hfnjvi6aymbsnfc2xeeoheizda'
